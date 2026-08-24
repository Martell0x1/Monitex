import json

from payloads import (
    build_anomaly_payload,
    coerce_payload,
    normalize_sensor_payload,
)


class _FakeRuntime:
    """Stand-in for AnomalyModelRuntime so payload tests don't need a real
    trained model - just fixed threshold values to check severity math."""

    def __init__(self, upper_threshold=None, max_valid_reading=None):
        self._upper = upper_threshold
        self._max_valid = max_valid_reading

    def upper_threshold(self, device_name=None, sensor_type=None):
        return self._upper

    def max_valid_reading(self, device_name=None, sensor_type=None):
        return self._max_valid


def _reading(value, device="esp32-1", sensor_type="ldr"):
    return {
        "deviceName": device,
        "sensorType": sensor_type,
        "sensorId": "s1",
        "sensorName": "Living room LDR",
        "ipAddress": "10.0.0.5",
        "timestamp": "2026-01-01T00:00:00Z",
        "value": value,
    }


def test_coerce_payload_rejects_non_json():
    assert coerce_payload(b"not json") is None
    assert coerce_payload(b'"just a string"') is None
    assert coerce_payload(json.dumps({"a": 1}).encode()) == {"a": 1}


def test_normalize_sensor_payload_accepts_valid_reading():
    payload = {
        "messageType": "sensor",
        "deviceName": "esp32-1",
        "sensorType": "ldr",
        "value": "42.5",
        "timestamp": "2026-01-01T00:00:00Z",
    }
    reading = normalize_sensor_payload(payload)

    assert reading["deviceName"] == "esp32-1"
    assert reading["sensorType"] == "ldr"
    assert reading["value"] == 42.5


def test_normalize_sensor_payload_rejects_non_sensor_message():
    payload = {"messageType": "health", "deviceName": "esp32-1", "value": 1}
    assert normalize_sensor_payload(payload) is None


def test_normalize_sensor_payload_rejects_missing_device_name():
    payload = {"messageType": "sensor", "value": 42}
    assert normalize_sensor_payload(payload) is None


def test_normalize_sensor_payload_rejects_non_numeric_value():
    payload = {"messageType": "sensor", "deviceName": "esp32-1", "value": "not-a-number"}
    assert normalize_sensor_payload(payload) is None


def test_severity_critical_beyond_hard_cap():
    runtime = _FakeRuntime(upper_threshold=40, max_valid_reading=50)
    payload = build_anomaly_payload(_reading(60), runtime, score=None)
    assert payload["severity"] == "critical"


def test_severity_warning_beyond_threshold_but_below_hard_cap():
    runtime = _FakeRuntime(upper_threshold=40, max_valid_reading=50)
    payload = build_anomaly_payload(_reading(45), runtime, score=None)
    assert payload["severity"] == "warning"


def test_severity_graded_by_score_within_bounds():
    runtime = _FakeRuntime(upper_threshold=40, max_valid_reading=50)

    critical = build_anomaly_payload(_reading(30), runtime, score=-0.2)
    warning = build_anomaly_payload(_reading(30), runtime, score=-0.1)
    info = build_anomaly_payload(_reading(30), runtime, score=0.1)

    assert critical["severity"] == "critical"
    assert warning["severity"] == "warning"
    assert info["severity"] == "info"


def test_anomaly_score_included_in_payload():
    runtime = _FakeRuntime(upper_threshold=40, max_valid_reading=50)
    payload = build_anomaly_payload(_reading(45), runtime, score=-0.3)
    assert payload["anomalyScore"] == -0.3