from config import (
    ANOMALY_RESULTS_ROUTING_KEY,
    MODEL_PATH,
    RABBITMQ_EXCHANGE_NAME,
    RABBITMQ_QUEUE_NAME,
    RABBITMQ_URL,
)
from model_runtime import AnomalyModelRuntime
from payloads import (
    build_anomaly_payload,
    coerce_payload,
    format_message,
    normalize_sensor_payload,
)
from rabbitmq_transport import RabbitMQAnomalyTransport


class AnomalyReaderService:
    def __init__(self) -> None:
        self.runtime = AnomalyModelRuntime(MODEL_PATH)
        self.transport = RabbitMQAnomalyTransport(
            rabbitmq_url=RABBITMQ_URL,
            queue_name=RABBITMQ_QUEUE_NAME,
            exchange_name=RABBITMQ_EXCHANGE_NAME,
            results_routing_key=ANOMALY_RESULTS_ROUTING_KEY,
        )

    async def consume(self) -> None:
        connection, channel = await self.transport.connect()
        queue = await self.transport.declare_input_queue(channel)
        exchange = await self.transport.get_exchange(channel)

        print(f"[RabbitMQ Reader] Listening on queue: {RABBITMQ_QUEUE_NAME}")
        print(f"[RabbitMQ Reader] Loaded model: {MODEL_PATH}")
        print(
            "[RabbitMQ Reader] Publishing anomaly results to "
            f"{RABBITMQ_EXCHANGE_NAME}:{ANOMALY_RESULTS_ROUTING_KEY}"
        )

        async with connection:
            async with queue.iterator() as queue_iter:
                async for message in queue_iter:
                    async with message.process(requeue=False):
                        formatted_message = format_message(message.body)
                        print(f"[RabbitMQ Reader] Received: {formatted_message}")

                        payload = coerce_payload(message.body)
                        if payload is None:
                            print("[RabbitMQ Reader] Skipped non-JSON payload.")
                            continue

                        reading = normalize_sensor_payload(payload)
                        if reading is None:
                            print(
                                "[RabbitMQ Reader] Skipped payload because it is not a usable sensor reading."
                            )
                            continue

                        prediction = self.runtime.predict(float(reading["value"]))
                        if prediction != -1:
                            print(
                                "[RabbitMQ Reader] Reading considered normal "
                                f"for {reading['deviceName']} / {reading['sensorType']}."
                            )
                            continue

                        anomaly_payload = build_anomaly_payload(reading, self.runtime)
                        await self.transport.publish_anomaly(exchange, anomaly_payload)

                        print(
                            "[RabbitMQ Reader] Anomaly published to RabbitMQ: "
                            f"{anomaly_payload}"
                        )
