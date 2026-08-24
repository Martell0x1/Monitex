from __future__ import annotations

import shutil
import sys
from datetime import datetime, timezone
from pathlib import Path

import joblib
import pandas as pd
from influxdb_client import InfluxDBClient
from sklearn.ensemble import IsolationForest
from sklearn.preprocessing import StandardScaler

sys.path.insert(0, str(Path(__file__).resolve().parent / "model"))

from config import (  # noqa: E402
    INFLUX_BUCKET,
    INFLUX_ORG,
    INFLUX_TOKEN,
    INFLUX_URL,
    DRIFT_WARN_RATIO,
    MODEL_PATH,
    TRAIN_CONTAMINATION,
    TRAIN_FIELD,
    TRAIN_IQR_MULTIPLIER,
    TRAIN_LOOKBACK_RANGE,
    TRAIN_MAX_VALID_READING,
    TRAIN_MEASUREMENT,
    TRAIN_MIN_SAMPLES,
    TRAIN_USE_RATE_OF_CHANGE,
)


def model_key(device_name: str, sensor_type: str) -> str:
    return f"{device_name}::{sensor_type}"


def _load_previous_profiles(model_output: str) -> dict[str, dict]:
  
    path = Path(model_output)
    if not path.exists():
        return {}

    try:
        from model_runtime import AnomalyModelRuntime

        bundle = joblib.load(path)
        normalized = AnomalyModelRuntime._normalize_bundle(bundle)
        return {
            key: entry[2]
            for key, entry in normalized.items()
            if entry[2] is not None
        }
    except Exception:
        return {}


def _check_drift(key: str, previous_profiles: dict[str, dict], new_profile: dict) -> str | None:
    """Returns a human-readable warning if the learned range moved by more
    than DRIFT_WARN_RATIO of the previous range's width, else None."""

    previous = previous_profiles.get(key)
    if previous is None:
        return None

    old_width = previous["upper_bound"] - previous["lower_bound"]
    if old_width <= 0:
        return None

    old_center = (previous["upper_bound"] + previous["lower_bound"]) / 2
    new_center = (new_profile["upper_bound"] + new_profile["lower_bound"]) / 2

    shift_ratio = abs(new_center - old_center) / old_width

    if shift_ratio > DRIFT_WARN_RATIO:
        return (
            f"{key}: learned range moved from "
            f"({previous['lower_bound']:.2f}, {previous['upper_bound']:.2f}) to "
            f"({new_profile['lower_bound']:.2f}, {new_profile['upper_bound']:.2f}) "
            f"- a {shift_ratio:.0%} shift relative to the previous range width"
        )

    return None


def load_all_sensor_data() -> pd.DataFrame:
    if not INFLUX_TOKEN:
        raise RuntimeError(
            "INFLUX_TOKEN is not set. Export it (or set it in the environment/"
            "docker-compose) instead of hardcoding it in source."
        )

    print("Connecting to InfluxDB...")

    client = InfluxDBClient(url=INFLUX_URL, token=INFLUX_TOKEN, org=INFLUX_ORG)

    try:
        query_api = client.query_api()

        flux_query = f"""
        from(bucket: "{INFLUX_BUCKET}")
          |> range(start: {TRAIN_LOOKBACK_RANGE})
          |> filter(fn: (r) => r["_measurement"] == "{TRAIN_MEASUREMENT}")
          |> filter(fn: (r) => r["_field"] == "{TRAIN_FIELD}")
          |> keep(columns: ["_time", "_value", "device_name", "sensorType"])
          |> sort(columns: ["_time"])
        """

        print("Fetching historical data for all devices/sensors...")
        df = query_api.query_data_frame(flux_query)
    finally:
        client.close()

    if isinstance(df, list):
        df = pd.concat(df) if df else pd.DataFrame(
            columns=["_time", "_value", "device_name", "sensorType"]
        )

    expected_cols = ["_time", "_value", "device_name", "sensorType"]
    if df.empty or not set(expected_cols).issubset(df.columns):
        return pd.DataFrame(columns=expected_cols)

    df = df[expected_cols].copy()
    df["_time"] = pd.to_datetime(df["_time"])
    df["_value"] = pd.to_numeric(df["_value"], errors="coerce")
    df.dropna(subset=["_value", "device_name", "sensorType"], inplace=True)

    print(f"Loaded {len(df)} samples across {df.groupby(['device_name', 'sensorType']).ngroups} series")

    return df


def fit_series(df: pd.DataFrame) -> tuple[IsolationForest, StandardScaler, dict] | None:
    df = df[df["_value"] <= TRAIN_MAX_VALID_READING].copy()

    if df.empty:
        return None

    q1 = df["_value"].quantile(0.25)
    q3 = df["_value"].quantile(0.75)
    iqr = q3 - q1

    lower_bound = max(0.0, q1 - (TRAIN_IQR_MULTIPLIER * iqr))
    upper_bound = min(TRAIN_MAX_VALID_READING, q3 + (TRAIN_IQR_MULTIPLIER * iqr))
    hard_cap = min(TRAIN_MAX_VALID_READING, max(upper_bound, df["_value"].quantile(0.995)))

    filtered = df[df["_value"].between(lower_bound, upper_bound)].copy()

    if filtered.empty:
        return None

    feature_cols = ["_value"]

    if TRAIN_USE_RATE_OF_CHANGE:
        filtered = filtered.sort_values("_time")
        filtered["_delta"] = filtered["_value"].diff().fillna(0.0)
        feature_cols = ["_value", "_delta"]

    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(filtered[feature_cols])

    model = IsolationForest(contamination=TRAIN_CONTAMINATION, random_state=42)
    model.fit(X_scaled)

    training_profile = {
        "lower_bound": float(lower_bound),
        "upper_bound": float(upper_bound),
        "max_valid_reading": float(hard_cap),
        "trained_at": datetime.now(timezone.utc).isoformat(),
        "sample_count": int(len(filtered)),
        "uses_rate_of_change": TRAIN_USE_RATE_OF_CHANGE,
    }

    return model, scaler, training_profile



def save_models(models: dict, model_output: str = MODEL_PATH) -> None:
    output_path = Path(model_output)

    if output_path.exists():
        backup_path = output_path.with_suffix(output_path.suffix + ".bak")
        shutil.copy2(output_path, backup_path)

    joblib.dump(models, output_path)
    print(f"Model(s) saved successfully -> {output_path} ({len(models)} series)")


def train_and_save_model(model_output: str = MODEL_PATH) -> dict:
    df = load_all_sensor_data()

    if df.empty:
        raise ValueError("No sensor data found in InfluxDB for the configured lookback range")

    previous_profiles = _load_previous_profiles(model_output)

    models: dict[str, tuple] = {}
    trained: list[dict] = []
    skipped: list[dict] = []
    drift_warnings: list[str] = []

    for (device_name, sensor_type), group in df.groupby(["device_name", "sensorType"]):
        key = model_key(device_name, sensor_type)

        if len(group) < TRAIN_MIN_SAMPLES:
            skipped.append(
                {"key": key, "reason": "not enough samples", "sample_count": len(group)}
            )
            continue

        fitted = fit_series(group)
        if fitted is None:
            skipped.append({"key": key, "reason": "no samples left after filtering"})
            continue

        model, scaler, profile = fitted
        models[key] = (model, scaler, profile)
        trained.append({"key": key, **profile})
        print(
            f"Trained {key}: {profile['sample_count']} samples, "
            f"normal range ({profile['lower_bound']:.2f}, {profile['upper_bound']:.2f})"
        )

        drift = _check_drift(key, previous_profiles, profile)
        if drift:
            drift_warnings.append(drift)
            print(f"[DRIFT WARNING] {drift}")

    if not models:
        raise ValueError(
            "No series had enough data to train a model. "
            f"Need at least {TRAIN_MIN_SAMPLES} samples per (device, sensorType)."
        )

    save_models(models, model_output=model_output)

    return {
        "model_path": str(model_output),
        "trained_series": trained,
        "skipped_series": skipped,
        "drift_warnings": drift_warnings,
        "total_samples": int(len(df)),
    }


def main() -> None:
    result = train_and_save_model()

    print(f"\nTrained {len(result['trained_series'])} series, "
          f"skipped {len(result['skipped_series'])}:")

    for entry in result["trained_series"]:
        print(f"  [OK]      {entry['key']}: {entry['sample_count']} samples")

    for entry in result["skipped_series"]:
        print(f"  [SKIPPED] {entry['key']}: {entry['reason']}")

    if result["drift_warnings"]:
        print("\nDrift warnings:")
        for warning in result["drift_warnings"]:
            print(f"  [DRIFT] {warning}")


if __name__ == "__main__":
    main()