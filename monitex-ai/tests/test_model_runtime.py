import joblib
import numpy as np
import pandas as pd
from sklearn.ensemble import IsolationForest
from sklearn.preprocessing import StandardScaler

from model_runtime import AnomalyModelRuntime


def _fit_one(values, lower, upper, max_valid):
    df = pd.DataFrame({"_value": values})
    scaler = StandardScaler().fit(df[["_value"]])
    model = IsolationForest(contamination=0.01, random_state=1).fit(
        scaler.transform(df[["_value"]])
    )
    profile = {
        "lower_bound": lower,
        "upper_bound": upper,
        "max_valid_reading": max_valid,
        "trained_at": "2026-01-01T00:00:00Z",
        "sample_count": len(values),
    }
    return model, scaler, profile


def _write_bundle(tmp_path, bundle, name="model.joblib"):
    path = tmp_path / name
    joblib.dump(bundle, path)
    return str(path)


def test_legacy_single_tuple_bundle_loads_as_default(tmp_path):
    rng = np.random.default_rng(0)
    values = 20 + rng.random(200) * 20
    entry = _fit_one(values, 18, 42, 42)

    path = _write_bundle(tmp_path, entry)
    runtime = AnomalyModelRuntime(path)

    assert runtime.known_series() == []
    assert runtime.predict(30) == 1
    assert runtime.predict(1000) == -1


def test_per_series_isolation(tmp_path):
    rng = np.random.default_rng(1)
    ldr_values = 20 + rng.random(200) * 20          # ~20-40
    temp_values = 60 + rng.random(200) * 20         # ~60-80

    bundle = {
        "esp32-1::ldr": _fit_one(ldr_values, 18, 42, 42),
        "esp32-1::temperature": _fit_one(temp_values, 58, 82, 82),
    }
    path = _write_bundle(tmp_path, bundle)
    runtime = AnomalyModelRuntime(path)

    assert set(runtime.known_series()) == {"esp32-1::ldr", "esp32-1::temperature"}

    assert runtime.predict(70, "esp32-1", "ldr") == -1
    assert runtime.predict(70, "esp32-1", "temperature") == 1


def test_unknown_device_returns_none_not_a_guess(tmp_path):
    rng = np.random.default_rng(2)
    values = 20 + rng.random(200) * 20
    bundle = {"esp32-1::ldr": _fit_one(values, 18, 42, 42)}
    path = _write_bundle(tmp_path, bundle)
    runtime = AnomalyModelRuntime(path)

    assert runtime.predict(30, "esp32-99", "ldr") is None


def test_reload_swaps_in_new_bundle_atomically(tmp_path):
    rng = np.random.default_rng(3)
    old_values = 20 + rng.random(200) * 20
    new_values = 20 + rng.random(200) * 20

    path = _write_bundle(
        tmp_path, {"esp32-1::ldr": _fit_one(old_values, 18, 42, 42)}
    )
    runtime = AnomalyModelRuntime(path)
    assert runtime.upper_threshold("esp32-1", "ldr") == 42

    joblib.dump({"esp32-1::ldr": _fit_one(new_values, 50, 90, 90)}, path)
    runtime.reload(path)

    assert runtime.upper_threshold("esp32-1", "ldr") == 90


def test_score_is_lower_for_more_anomalous_values(tmp_path):
    rng = np.random.default_rng(4)
    values = 20 + rng.random(200) * 20
    bundle = {"esp32-1::ldr": _fit_one(values, 18, 42, 42)}
    path = _write_bundle(tmp_path, bundle)
    runtime = AnomalyModelRuntime(path)

    normal_score = runtime.score(30, "esp32-1", "ldr")
    anomalous_score = runtime.score(1000, "esp32-1", "ldr")

    assert anomalous_score < normal_score


def _fit_with_delta(values, lower, upper, max_valid):
    """Like _fit_one but trains on [_value, _delta] the way train_model.py
    does when TRAIN_USE_RATE_OF_CHANGE is on."""
    df = pd.DataFrame({"_value": values})
    df["_delta"] = df["_value"].diff().fillna(0.0)
    scaler = StandardScaler().fit(df[["_value", "_delta"]])
    model = IsolationForest(contamination=0.01, random_state=1).fit(
        scaler.transform(df[["_value", "_delta"]])
    )
    profile = {
        "lower_bound": lower,
        "upper_bound": upper,
        "max_valid_reading": max_valid,
        "trained_at": "2026-01-01T00:00:00Z",
        "sample_count": len(values),
        "uses_rate_of_change": True,
    }
    return model, scaler, profile


def test_observe_returns_zero_delta_for_first_reading(tmp_path):
    rng = np.random.default_rng(5)
    base = 30 + rng.normal(scale=0.5, size=300)
    bundle = {"esp32-1::ldr": _fit_with_delta(base, 25, 45, 45)}
    path = _write_bundle(tmp_path, bundle)
    runtime = AnomalyModelRuntime(path)

    delta = runtime.observe(30.0, "esp32-1", "ldr")
    assert delta == 0.0


def test_sudden_jump_scores_worse_than_gradual_climb_to_same_value(tmp_path):

    import train_model

    rng = np.random.default_rng(6)
    rows = []
    value = 30.0
    for i in range(300):
        value += rng.standard_normal() * 0.5
        rows.append(
            {
                "_time": pd.Timestamp("2026-01-01") + pd.Timedelta(minutes=i),
                "_value": value,
                "device_name": "esp32-1",
                "sensorType": "ldr",
            }
        )
    df = pd.DataFrame(rows)

    model_path = str(tmp_path / "model.joblib")
    original_loader = train_model.load_all_sensor_data
    train_model.load_all_sensor_data = lambda: df
    try:
        train_model.train_and_save_model(model_output=model_path)
    finally:
        train_model.load_all_sensor_data = original_loader

    jump_runtime = AnomalyModelRuntime(model_path)
    jump_runtime.observe(30.0, "esp32-1", "ldr")
    jump_delta = jump_runtime.observe(39.0, "esp32-1", "ldr")
    jump_score = jump_runtime.score(39.0, "esp32-1", "ldr", delta=jump_delta)

    gradual_runtime = AnomalyModelRuntime(model_path)
    step_value = 30.0
    gradual_runtime.observe(step_value, "esp32-1", "ldr")
    gradual_score = None
    for _ in range(9):
        step_value += 1.0
        delta = gradual_runtime.observe(step_value, "esp32-1", "ldr")
        gradual_score = gradual_runtime.score(step_value, "esp32-1", "ldr", delta=delta)

    assert jump_score < gradual_score


def test_legacy_profile_without_rate_of_change_flag_ignores_delta(tmp_path):
    rng = np.random.default_rng(7)
    values = 20 + rng.random(200) * 20
    # No "uses_rate_of_change" key at all - old-style profile.
    bundle = {"esp32-1::ldr": _fit_one(values, 18, 42, 42)}
    path = _write_bundle(tmp_path, bundle)
    runtime = AnomalyModelRuntime(path)

    delta = runtime.observe(30.0, "esp32-1", "ldr")
    assert runtime.predict(30.0, "esp32-1", "ldr", delta=9999.0) == \
        runtime.predict(30.0, "esp32-1", "ldr", delta=0.0)