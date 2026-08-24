import asyncio
import json
import sys
import time
import traceback
from datetime import datetime, timezone
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent.parent))

from config import (
    ANOMALY_NOTIFICATION_COOLDOWN_SECONDS,
    ANOMALY_RESULTS_ROUTING_KEY,
    FALLBACK_MIN_SAMPLES,
    FALLBACK_Z_THRESHOLD,
    HEALTH_CHECK_PORT,
    MODEL_PATH,
    RABBITMQ_EXCHANGE_NAME,
    RABBITMQ_QUEUE_NAME,
    RABBITMQ_RETRAIN_QUEUE_NAME,
    RABBITMQ_URL,
    RETRAIN_INTERVAL_MINUTES,
    RETRAIN_ROUTING_KEY,
    RETRAIN_SCHEDULER_ENABLED,
)
from fallback_detector import FallbackAnomalyDetector
from health_server import HealthStatus, start_health_server
from model_runtime import AnomalyModelRuntime
from payloads import (
    build_anomaly_payload,
    coerce_payload,
    format_message,
    normalize_sensor_payload,
)
from rabbitmq_transport import RabbitMQAnomalyTransport
from train_model import train_and_save_model


class AnomalyReaderService:
    def __init__(self) -> None:
        self.runtime = AnomalyModelRuntime(MODEL_PATH)
        self.transport = RabbitMQAnomalyTransport(
            rabbitmq_url=RABBITMQ_URL,
            queue_name=RABBITMQ_QUEUE_NAME,
            exchange_name=RABBITMQ_EXCHANGE_NAME,
            results_routing_key=ANOMALY_RESULTS_ROUTING_KEY,
            retrain_queue_name=RABBITMQ_RETRAIN_QUEUE_NAME,
            retrain_routing_key=RETRAIN_ROUTING_KEY,
        )
        self._retrain_lock = asyncio.Lock()
        self._last_notified: dict[str, tuple[float, str]] = {}
        self._fallback = FallbackAnomalyDetector(
            min_samples=FALLBACK_MIN_SAMPLES, z_threshold=FALLBACK_Z_THRESHOLD
        )
        self._health = HealthStatus()
        self._health.update(status="starting", known_series=self.runtime.known_series())

    _SEVERITY_RANK = {"info": 0, "warning": 1, "critical": 2}

    async def consume(self) -> None:
       

        connection, channel = await self.transport.connect()
        retrain_channel = await connection.channel()
        await retrain_channel.set_qos(prefetch_count=1)

        start_health_server(self._health, HEALTH_CHECK_PORT)
        self._health.update(status="ok")
        print(f"[Health] Serving GET /health on port {HEALTH_CHECK_PORT}")

        async with connection:
            await asyncio.gather(
                self._consume_anomalies(channel),
                self._consume_retrain_events(retrain_channel),
                self._run_scheduled_retrain(),
            )

    async def _consume_anomalies(self, channel) -> None:
        queue = await self.transport.declare_input_queue(channel)
        exchange = await self.transport.get_exchange(channel)

        print(f"[RabbitMQ Reader] Listening on queue: {RABBITMQ_QUEUE_NAME}")
        print(f"[RabbitMQ Reader] Loaded model: {MODEL_PATH}")
        known = self.runtime.known_series()
        if known:
            print(f"[RabbitMQ Reader] Known trained series: {known}")
        print(
            "[RabbitMQ Reader] Publishing anomaly results to "
            f"{RABBITMQ_EXCHANGE_NAME}:{ANOMALY_RESULTS_ROUTING_KEY}"
        )

        async with queue.iterator() as queue_iter:
            async for message in queue_iter:
                async with message.process(requeue=False):
                    try:
                        await self._handle_sensor_message(message.body, exchange)
                    except Exception:
                        print("[RabbitMQ Reader] Error handling sensor message:")
                        traceback.print_exc()

    async def _handle_sensor_message(self, body: bytes, exchange) -> None:
        formatted_message = format_message(body)
        print(f"[RabbitMQ Reader] Received: {formatted_message}")

        payload = coerce_payload(body)
        if payload is None:
            print("[RabbitMQ Reader] Skipped non-JSON payload.")
            return

        reading = normalize_sensor_payload(payload)
        if reading is None:
            print(
                "[RabbitMQ Reader] Skipped payload because it is not a usable sensor reading."
            )
            return

        self._health.increment("readings_processed")

        value = float(reading["value"])
        device_name = reading["deviceName"]
        sensor_type = reading["sensorType"]

        delta = self.runtime.observe(value, device_name, sensor_type)

        prediction = self.runtime.predict(value, device_name, sensor_type, delta=delta)

        if prediction is None:
            await self._handle_unmodeled_reading(reading, value, exchange)
            return

        if prediction != -1:
            print(
                "[RabbitMQ Reader] Reading considered normal "
                f"for {device_name} / {sensor_type}."
            )
            return

        anomaly_payload = build_anomaly_payload(
            reading,
            self.runtime,
            score=self.runtime.score(value, device_name, sensor_type, delta=delta),
        )

        if not self._should_notify(reading, anomaly_payload["severity"]):
            self._health.increment("notifications_suppressed_by_cooldown")
            print(
                "[RabbitMQ Reader] Anomaly detected but suppressed by cooldown "
                f"for {device_name} / {sensor_type} "
                f"(severity={anomaly_payload['severity']})."
            )
            return

        await self.transport.publish_anomaly(exchange, anomaly_payload)
        self._health.increment("anomalies_published")

        print(f"[RabbitMQ Reader] Anomaly published to RabbitMQ: {anomaly_payload}")

    async def _handle_unmodeled_reading(self, reading: dict, value: float, exchange) -> None:
      

        key = f"{reading['deviceName']}::{reading['sensorType']}"
        is_anomaly, z_score = self._fallback.observe(key, value)

        if not is_anomaly:
            score_label = f"{z_score:.2f}" if z_score is not None else "building baseline"
            print(
                f"[RabbitMQ Reader] No trained model yet for {key}; "
                f"fallback check: normal (z={score_label})."
            )
            return

        payload = {
            "deviceName": reading["deviceName"],
            "sensorId": reading["sensorId"],
            "sensorName": reading["sensorName"],
            "sensorType": reading["sensorType"],
            "severity": "warning",
            "title": "Sensor anomaly detected (provisional)",
            "message": (
                f"{reading['sensorType']} reading {value:.2f} looks unusual compared to "
                f"what this sensor has shown so far (z-score {z_score:.2f}). No trained "
                "model exists for it yet - this is a provisional check until the next "
                "scheduled retrain."
            ),
            "value": value,
            "threshold": None,
            "anomalyScore": z_score,
            "ipAddress": reading["ipAddress"],
            "timestamp": reading["timestamp"],
        }

        if not self._should_notify(reading, payload["severity"]):
            self._health.increment("notifications_suppressed_by_cooldown")
            print(f"[RabbitMQ Reader] Fallback anomaly suppressed by cooldown for {key}.")
            return

        await self.transport.publish_anomaly(exchange, payload)
        self._health.increment("anomalies_published")
        print(f"[RabbitMQ Reader] Fallback anomaly published to RabbitMQ: {payload}")

    def _should_notify(self, reading: dict, severity: str) -> bool:
        

        key = f"{reading['deviceName']}::{reading['sensorType']}"
        now = time.monotonic()
        last = self._last_notified.get(key)

        if last is not None:
            last_time, last_severity = last
            escalated = self._SEVERITY_RANK[severity] > self._SEVERITY_RANK[last_severity]
            within_cooldown = (now - last_time) < ANOMALY_NOTIFICATION_COOLDOWN_SECONDS

            if within_cooldown and not escalated:
                return False

        self._last_notified[key] = (now, severity)
        return True


    async def _retrain_and_reload(self) -> dict:
        async with self._retrain_lock:
            loop = asyncio.get_running_loop()
            result = await loop.run_in_executor(None, train_and_save_model, MODEL_PATH)
            self.runtime.reload(MODEL_PATH)
            self._health.update(
                last_retrain_at=datetime.now(timezone.utc).isoformat(),
                last_retrain_ok=True,
                last_drift_warnings=result.get("drift_warnings", []),
                known_series=self.runtime.known_series(),
            )
            return result

    async def _run_scheduled_retrain(self) -> None:
        if not RETRAIN_SCHEDULER_ENABLED:
            print("[Retrain Scheduler] Disabled via RETRAIN_SCHEDULER_ENABLED.")
            return

        if RETRAIN_INTERVAL_MINUTES <= 0:
            print(
                "[Retrain Scheduler] RETRAIN_INTERVAL_MINUTES must be > 0; "
                "internal scheduler will not run."
            )
            return

        interval_seconds = RETRAIN_INTERVAL_MINUTES * 60
        print(
            "[Retrain Scheduler] Internal scheduler started. "
            f"Interval: {RETRAIN_INTERVAL_MINUTES} minute(s)."
        )

        while True:
            await asyncio.sleep(interval_seconds)
            print("[Retrain Scheduler] Scheduled retrain starting...")
            try:
                result = await self._retrain_and_reload()
                print(
                    "[Retrain Scheduler] Retrain complete. "
                    f"trained={len(result['trained_series'])} "
                    f"skipped={len(result['skipped_series'])} "
                    f"total_samples={result['total_samples']}"
                )
                for warning in result.get("drift_warnings", []):
                    print(f"[Retrain Scheduler] [DRIFT WARNING] {warning}")
            except Exception:
                print("[Retrain Scheduler] Scheduled retrain failed:")
                traceback.print_exc()
                self._health.update(
                    last_retrain_at=datetime.now(timezone.utc).isoformat(),
                    last_retrain_ok=False,
                )

    async def _consume_retrain_events(self, channel) -> None:
        queue = await self.transport.declare_retrain_queue(channel)

        print(
            f"[Retrain Listener] Listening on queue: {RABBITMQ_RETRAIN_QUEUE_NAME} "
            f"(routing key: {RETRAIN_ROUTING_KEY}) - optional, nothing needs to publish here."
        )

        async with queue.iterator() as queue_iter:
            async for message in queue_iter:
                async with message.process(requeue=False):
                    try:
                        await self._handle_retrain_event(message.body)
                    except Exception:
                        print("[Retrain Listener] Retrain run failed:")
                        traceback.print_exc()

    async def _handle_retrain_event(self, body: bytes) -> None:
        try:
            trigger = json.loads(body.decode("utf-8"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            trigger = {}

        print(f"[Retrain Listener] On-demand retrain triggered: {trigger}")

        result = await self._retrain_and_reload()

        print(
            "[Retrain Listener] Model retrained and hot-reloaded. "
            f"trained={len(result['trained_series'])} "
            f"skipped={len(result['skipped_series'])} "
            f"total_samples={result['total_samples']}"
        )
        for warning in result.get("drift_warnings", []):
            print(f"[Retrain Listener] [DRIFT WARNING] {warning}")