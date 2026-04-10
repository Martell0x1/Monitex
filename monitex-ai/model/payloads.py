import json
from datetime import datetime, timezone
from typing import Any

from model_runtime import AnomalyModelRuntime


def format_message(raw: bytes) -> str:
    text = raw.decode("utf-8", errors="replace")
    try:
        payload = json.loads(text)
        return json.dumps(payload, ensure_ascii=False)
    except json.JSONDecodeError:
        return text


def coerce_payload(raw: bytes) -> dict[str, Any] | None:
    try:
        payload = json.loads(raw.decode("utf-8"))
    except (UnicodeDecodeError, json.JSONDecodeError):
        return None

    return payload if isinstance(payload, dict) else None


def pick(payload: dict[str, Any], *keys: str) -> Any:
    for key in keys:
        if key in payload and payload[key] is not None:
            return payload[key]
    return None


def normalize_sensor_payload(payload: dict[str, Any]) -> dict[str, Any] | None:
    message_type = str(pick(payload, "messageType", "MessageType") or "sensor").strip().lower()
    if message_type != "sensor":
        return None

    raw_value = pick(payload, "value", "Value")

    try:
        numeric_value = float(raw_value)
    except (TypeError, ValueError):
        return None

    device_name = pick(payload, "deviceName", "DeviceName")
    if not device_name:
        return None

    timestamp = pick(payload, "timestamp", "Timestamp")

    return {
        "deviceName": str(device_name),
        "sensorType": str(pick(payload, "sensorType", "SensorType") or "unknown"),
        "sensorId": pick(payload, "sensorId", "SensorId"),
        "sensorName": pick(payload, "sensorName", "SensorName"),
        "ipAddress": pick(payload, "ipAddress", "IpAddress"),
        "timestamp": str(timestamp) if timestamp else datetime.now(timezone.utc).isoformat(),
        "value": numeric_value,
    }


def build_anomaly_payload(
    reading: dict[str, Any],
    runtime: AnomalyModelRuntime,
) -> dict[str, Any]:
    value = float(reading["value"])
    threshold = runtime.upper_threshold()
    max_valid = runtime.max_valid_reading()

    severity = "warning"
    if max_valid is not None and value > max_valid:
        severity = "critical"
    elif threshold is not None and value > threshold:
        severity = "critical"

    threshold_label = f"{threshold:.2f}" if threshold is not None else "the expected range"

    return {
        "deviceName": reading["deviceName"],
        "sensorId": reading["sensorId"],
        "sensorName": reading["sensorName"],
        "sensorType": reading["sensorType"],
        "severity": severity,
        "title": "Sensor anomaly detected",
        "message": (
            f"{reading['sensorType']} reading {value:.2f} exceeded the learned normal range "
            f"(threshold: {threshold_label})."
        ),
        "value": value,
        "threshold": threshold,
        "ipAddress": reading["ipAddress"],
        "timestamp": reading["timestamp"],
    }
