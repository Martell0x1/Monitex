from __future__ import annotations

import joblib
import pandas as pd
from influxdb_client import InfluxDBClient
from sklearn.ensemble import IsolationForest
from sklearn.preprocessing import StandardScaler

# ======================
# CONFIGURATION
# ======================

INFLUX_URL = "http://localhost:8086"
INFLUX_TOKEN = "E_0cWP_5aLZ6KQKK7y6rPVXvoI1DRF6bf0xVjMrw2JwUCu6McyG4mQe6y629aHh6Q9jNnalYkqq_HTzxDWU7nA=="
INFLUX_ORG = "monitex-org"
INFLUX_BUCKET = "monitex"

DEVICE_NAME = "esp32-1"
MEASUREMENT = "sensor_readings"
FIELD = "value"

LOOKBACK_RANGE = "-7d"

MODEL_OUTPUT = "esp32_anomaly_model.joblib"

CONTAMINATION = 0.01
MAX_VALID_READING = 40.0
IQR_MULTIPLIER = 1.5


# ======================
# LOAD DATA FROM INFLUX
# ======================

def load_sensor_data() -> pd.DataFrame:
    print("Connecting to InfluxDB...")

    client = InfluxDBClient(
        url=INFLUX_URL,
        token=INFLUX_TOKEN,
        org=INFLUX_ORG
    )

    query_api = client.query_api()

    flux_query = f"""
    from(bucket: "{INFLUX_BUCKET}")
      |> range(start: {LOOKBACK_RANGE})
      |> filter(fn: (r) => r["_measurement"] == "{MEASUREMENT}")
      |> filter(fn: (r) => r["_field"] == "{FIELD}")
      |> filter(fn: (r) => r["device_name"] == "{DEVICE_NAME}")
      |> keep(columns: ["_time", "_value"])
      |> sort(columns: ["_time"])
    """

    print("Fetching historical data...")

    df = query_api.query_data_frame(flux_query)

    client.close()

    if isinstance(df, list):
        df = pd.concat(df)

    df = df[["_time", "_value"]].copy()

    df["_time"] = pd.to_datetime(df["_time"])
    df["_value"] = pd.to_numeric(df["_value"], errors="coerce")

    df.dropna(inplace=True)

    print(f"Loaded {len(df)} samples")

    return df


# ======================
# TRAIN MODEL
# ======================
def train_model(df):
    print("Filtering obvious anomalies from training dataset...")

    df = df[df["_value"] <= MAX_VALID_READING].copy()

    if df.empty:
        raise ValueError("No training samples left after the initial value filter")

    q1 = df["_value"].quantile(0.25)
    q3 = df["_value"].quantile(0.75)
    iqr = q3 - q1

    lower_bound = max(0.0, q1 - (IQR_MULTIPLIER * iqr))
    upper_bound = min(MAX_VALID_READING, q3 + (IQR_MULTIPLIER * iqr))

    df = df[df["_value"].between(lower_bound, upper_bound)].copy()

    if df.empty:
        raise ValueError("No training samples left after IQR filtering")

    print(f"Training samples after filtering: {len(df)}")
    print(f"Learned normal range: {lower_bound:.2f} to {upper_bound:.2f}")

    scaler = StandardScaler()
    X_scaled = scaler.fit_transform(df[["_value"]])

    model = IsolationForest(
        contamination=0.01,
        random_state=42
    )

    model.fit(X_scaled)

    print("Model training complete")

    training_profile = {
        "lower_bound": float(lower_bound),
        "upper_bound": float(upper_bound),
        "max_valid_reading": MAX_VALID_READING,
    }

    return model, scaler, training_profile

def predict_value(model, scaler, value, training_profile=None):

    import pandas as pd

    if training_profile is not None:
        lower_bound = training_profile["lower_bound"]
        upper_bound = training_profile["upper_bound"]

        if value < lower_bound or value > upper_bound:
            return -1

    X = scaler.transform(pd.DataFrame([[value]], columns=["_value"]))

    prediction = model.predict(X)[0]

    return prediction

# ======================
# EVALUATE MODEL
# ======================

def evaluate_model(
    model: IsolationForest,
    scaler: StandardScaler,
    df: pd.DataFrame,
    training_profile: dict,
):
    print("Evaluating anomalies on training dataset...")

    scaled_values = scaler.transform(df[["_value"]])
    predictions = model.predict(scaled_values)

    out_of_range_mask = ~df["_value"].between(
        training_profile["lower_bound"],
        training_profile["upper_bound"],
    )
    predictions[out_of_range_mask.to_numpy()] = -1

    df["anomaly"] = predictions

    anomaly_count = (predictions == -1).sum()
    normal_count = (predictions == 1).sum()

    print(f"Normal points: {normal_count}")
    print(f"Anomalies detected: {anomaly_count}")


# ======================
# SAVE MODEL
# ======================

def save_model(model_bundle):
    joblib.dump(model_bundle, MODEL_OUTPUT)
    print(f"Model saved successfully → {MODEL_OUTPUT}")


# ======================
# TEST REAL-TIME INFERENCE
# ======================
def test_realtime_predictions(model, scaler, training_profile):
    print("\nTesting real-time predictions:\n")

    test_values = [22, 23, 24, 95, 21]

    for value in test_values:
        prediction = predict_value(model, scaler, value, training_profile)

        label = "ANOMALY" if prediction == -1 else "NORMAL"

        print(f"value={value} → {label}")

# ======================
# MAIN PIPELINE
# ======================

def main():
    df = load_sensor_data()

    if len(df) < 50:
        print("Not enough data to train model")
        return
    model, scaler, training_profile = train_model(df)

    evaluate_model(model, scaler, df.copy(), training_profile)

    save_model((model, scaler, training_profile))

    test_realtime_predictions(model, scaler, training_profile)

if __name__ == "__main__":
    main()
