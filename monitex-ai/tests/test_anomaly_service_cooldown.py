import anomaly_service


def _service(cooldown_seconds=60):
    anomaly_service.ANOMALY_NOTIFICATION_COOLDOWN_SECONDS = cooldown_seconds
    svc = anomaly_service.AnomalyReaderService.__new__(anomaly_service.AnomalyReaderService)
    svc._last_notified = {}
    return svc


def _reading():
    return {"deviceName": "esp32-1", "sensorType": "ldr"}


def test_first_notification_always_goes_through():
    svc = _service()
    assert svc._should_notify(_reading(), "warning") is True


def test_repeat_same_severity_within_cooldown_is_suppressed():
    svc = _service()
    assert svc._should_notify(_reading(), "warning") is True
    assert svc._should_notify(_reading(), "warning") is False


def test_escalation_bypasses_cooldown():
    svc = _service()
    assert svc._should_notify(_reading(), "warning") is True
    assert svc._should_notify(_reading(), "critical") is True


def test_deescalation_within_cooldown_is_still_suppressed():
    svc = _service()
    assert svc._should_notify(_reading(), "critical") is True
    assert svc._should_notify(_reading(), "warning") is False


def test_notification_allowed_again_after_cooldown_expires():
    svc = _service(cooldown_seconds=60)
    assert svc._should_notify(_reading(), "warning") is True

    # Fast-forward past the cooldown window without sleeping in the test.
    key = "esp32-1::ldr"
    last_time, last_severity = svc._last_notified[key]
    svc._last_notified[key] = (last_time - 61, last_severity)

    assert svc._should_notify(_reading(), "warning") is True


def test_cooldown_state_is_tracked_per_device_and_sensor():
    svc = _service()
    assert svc._should_notify({"deviceName": "esp32-1", "sensorType": "ldr"}, "warning") is True
    # A different sensor should not be affected by the first one's cooldown.
    assert svc._should_notify({"deviceName": "esp32-1", "sensorType": "temperature"}, "warning") is True
    assert svc._should_notify({"deviceName": "esp32-2", "sensorType": "ldr"}, "warning") is True