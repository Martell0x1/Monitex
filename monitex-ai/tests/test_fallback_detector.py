from fallback_detector import FallbackAnomalyDetector


def test_no_anomaly_before_min_samples():
    detector = FallbackAnomalyDetector(min_samples=20, z_threshold=3.0)

    for value in [30, 31, 29, 30, 900]:  # even a wild value early on
        is_anomaly, z_score = detector.observe("esp32-1::ldr", value)
        assert is_anomaly is False
        assert z_score is None


def test_flags_clear_outlier_once_baseline_established():
    detector = FallbackAnomalyDetector(min_samples=20, z_threshold=3.0)

    for value in [30, 31, 29, 30, 31, 29, 30, 31, 29, 30,
                  31, 29, 30, 31, 29, 30, 31, 29, 30, 31]:
        detector.observe("esp32-1::ldr", value)

    is_anomaly, z_score = detector.observe("esp32-1::ldr", 1000)
    assert is_anomaly is True
    assert z_score is not None
    assert z_score > 3.0


def test_normal_value_after_baseline_is_not_flagged():
    detector = FallbackAnomalyDetector(min_samples=20, z_threshold=3.0)

    for value in [30, 31, 29, 30, 31, 29, 30, 31, 29, 30,
                  31, 29, 30, 31, 29, 30, 31, 29, 30, 31]:
        detector.observe("esp32-1::ldr", value)

    is_anomaly, _ = detector.observe("esp32-1::ldr", 30.5)
    assert is_anomaly is False


def test_stats_are_isolated_per_key():
    detector = FallbackAnomalyDetector(min_samples=5, z_threshold=3.0)

    for value in [10, 10.2, 9.8, 10.1, 9.9]:
        detector.observe("esp32-1::ldr", value)
    for value in [500, 500.2, 499.8, 500.1, 499.9]:
        detector.observe("esp32-2::temperature", value)

    assert set(detector.known_keys()) == {"esp32-1::ldr", "esp32-2::temperature"}
    is_anomaly_1, _ = detector.observe("esp32-1::ldr", 500)
    is_anomaly_2, _ = detector.observe("esp32-2::temperature", 500)
    assert is_anomaly_1 is True
    assert is_anomaly_2 is False