import numpy as np
import pandas as pd
import pytest

import train_model


def _series_df(device, sensor_type, values, start="2026-01-01"):
    return pd.DataFrame(
        {
            "_time": pd.date_range(start, periods=len(values), freq="min"),
            "_value": values,
            "device_name": device,
            "sensorType": sensor_type,
        }
    )


def test_trains_one_model_per_series_and_skips_low_sample_series(monkeypatch, tmp_path):
    rng = np.random.default_rng(0)

    df = pd.concat(
        [
            _series_df("esp32-1", "ldr", 20 + rng.random(200) * 20),
            _series_df("esp32-1", "temperature", 60 + rng.random(200) * 20),
            _series_df("esp32-2", "ldr", [50] * 10),  # below TRAIN_MIN_SAMPLES
        ]
    )

    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: df)

    model_path = str(tmp_path / "model.joblib")
    result = train_model.train_and_save_model(model_output=model_path)

    trained_keys = {t["key"] for t in result["trained_series"]}
    skipped_keys = {s["key"] for s in result["skipped_series"]}

    assert trained_keys == {"esp32-1::ldr", "esp32-1::temperature"}
    assert skipped_keys == {"esp32-2::ldr"}
    assert result["drift_warnings"] == []


def test_raises_when_no_series_has_enough_data(monkeypatch, tmp_path):
    df = _series_df("esp32-1", "ldr", [20] * 5)  # below TRAIN_MIN_SAMPLES
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: df)

    with pytest.raises(ValueError):
        train_model.train_and_save_model(model_output=str(tmp_path / "model.joblib"))


def test_raises_when_no_data_at_all(monkeypatch, tmp_path):
    monkeypatch.setattr(
        train_model,
        "load_all_sensor_data",
        lambda: pd.DataFrame(columns=["_time", "_value", "device_name", "sensorType"]),
    )

    with pytest.raises(ValueError):
        train_model.train_and_save_model(model_output=str(tmp_path / "model.joblib"))


def test_drift_warning_fires_on_large_range_shift(monkeypatch, tmp_path):
    model_path = str(tmp_path / "model.joblib")

    rng = np.random.default_rng(1)
    baseline = _series_df("esp32-1", "ldr", 20 + rng.random(200) * 20)
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: baseline)
    first = train_model.train_and_save_model(model_output=model_path)
    assert first["drift_warnings"] == []

    shifted = _series_df("esp32-1", "ldr", 200 + rng.random(200) * 20)
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: shifted)
    second = train_model.train_and_save_model(model_output=model_path)

    assert len(second["drift_warnings"]) == 1
    assert "esp32-1::ldr" in second["drift_warnings"][0]


def test_no_drift_warning_on_stable_retrain(monkeypatch, tmp_path):
    model_path = str(tmp_path / "model.joblib")

    rng = np.random.default_rng(2)
    first_batch = _series_df("esp32-1", "ldr", 20 + rng.random(200) * 20)
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: first_batch)
    train_model.train_and_save_model(model_output=model_path)

    second_batch = _series_df("esp32-1", "ldr", 20 + rng.random(200) * 20)
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: second_batch)
    result = train_model.train_and_save_model(model_output=model_path)

    assert result["drift_warnings"] == []


def test_backup_file_created_on_second_save(monkeypatch, tmp_path):
    model_path = tmp_path / "model.joblib"

    rng = np.random.default_rng(3)
    df = _series_df("esp32-1", "ldr", 20 + rng.random(200) * 20)
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: df)

    train_model.train_and_save_model(model_output=str(model_path))
    assert not model_path.with_suffix(".joblib.bak").exists()

    train_model.train_and_save_model(model_output=str(model_path))
    assert model_path.with_suffix(".joblib.bak").exists()


def test_training_profile_records_rate_of_change_flag(monkeypatch, tmp_path):
    rng = np.random.default_rng(4)
    df = _series_df("esp32-1", "ldr", 30 + rng.normal(scale=0.5, size=300))
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: df)

    result = train_model.train_and_save_model(model_output=str(tmp_path / "model.joblib"))

    assert result["trained_series"][0]["uses_rate_of_change"] is True


def test_rate_of_change_disabled_falls_back_to_value_only(monkeypatch, tmp_path):
    monkeypatch.setattr(train_model, "TRAIN_USE_RATE_OF_CHANGE", False)

    rng = np.random.default_rng(5)
    df = _series_df("esp32-1", "ldr", 30 + rng.normal(scale=0.5, size=300))
    monkeypatch.setattr(train_model, "load_all_sensor_data", lambda: df)

    result = train_model.train_and_save_model(model_output=str(tmp_path / "model.joblib"))

    assert result["trained_series"][0]["uses_rate_of_change"] is False