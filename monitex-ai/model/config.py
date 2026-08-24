import os
from pathlib import Path

RABBITMQ_URL = os.getenv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672/")
RABBITMQ_QUEUE_NAME = os.getenv("RABBITMQ_QUEUE_NAME", "iot.sensors.anomaly.queue")
RABBITMQ_EXCHANGE_NAME = os.getenv("RABBITMQ_EXCHANGE_NAME", "iot.sensors.exchange")
ANOMALY_RESULTS_ROUTING_KEY = os.getenv(
    "ANOMALY_RESULTS_ROUTING_KEY",
    "sensor.anomaly.detected",
)

RABBITMQ_RETRAIN_QUEUE_NAME = os.getenv(
    "RABBITMQ_RETRAIN_QUEUE_NAME", "iot.model.retrain.queue"
)
RETRAIN_ROUTING_KEY = os.getenv("RETRAIN_ROUTING_KEY", "model.retrain")

RETRAIN_SCHEDULER_ENABLED = os.getenv("RETRAIN_SCHEDULER_ENABLED", "true").strip().lower() in (
    "1",
    "true",
    "yes",
)
RETRAIN_INTERVAL_MINUTES = int(os.getenv("RETRAIN_INTERVAL_MINUTES", "1440"))

MODEL_PATH = os.getenv(
    "ANOMALY_MODEL_PATH",
    str(Path(__file__).resolve().parent.parent / "esp32_anomaly_model.joblib"),
)


INFLUX_URL = os.getenv("INFLUX_URL", "http://localhost:8086")
INFLUX_TOKEN = os.getenv("INFLUX_TOKEN", "")
INFLUX_ORG = os.getenv("INFLUX_ORG", "monitex-org")
INFLUX_BUCKET = os.getenv("INFLUX_BUCKET", "monitex")
TRAIN_MEASUREMENT = os.getenv("TRAIN_MEASUREMENT", "sensor_readings")
TRAIN_FIELD = os.getenv("TRAIN_FIELD", "value")
TRAIN_LOOKBACK_RANGE = os.getenv("TRAIN_LOOKBACK_RANGE", "-7d")
TRAIN_CONTAMINATION = float(os.getenv("TRAIN_CONTAMINATION", "0.01"))
TRAIN_MAX_VALID_READING = float(os.getenv("TRAIN_MAX_VALID_READING", "1000000"))
TRAIN_IQR_MULTIPLIER = float(os.getenv("TRAIN_IQR_MULTIPLIER", "1.5"))
TRAIN_MIN_SAMPLES = int(os.getenv("TRAIN_MIN_SAMPLES", "50"))

ANOMALY_NOTIFICATION_COOLDOWN_SECONDS = int(
    os.getenv("ANOMALY_NOTIFICATION_COOLDOWN_SECONDS", "300")
)

DRIFT_WARN_RATIO = float(os.getenv("DRIFT_WARN_RATIO", "0.5"))


TRAIN_USE_RATE_OF_CHANGE = os.getenv("TRAIN_USE_RATE_OF_CHANGE", "true").strip().lower() in (
    "1",
    "true",
    "yes",
)

FALLBACK_MIN_SAMPLES = int(os.getenv("FALLBACK_MIN_SAMPLES", "20"))
FALLBACK_Z_THRESHOLD = float(os.getenv("FALLBACK_Z_THRESHOLD", "3.5"))

HEALTH_CHECK_PORT = int(os.getenv("HEALTH_CHECK_PORT", "8000"))

SEVERITY_SCORE_CRITICAL = float(os.getenv("SEVERITY_SCORE_CRITICAL", "-0.15"))
SEVERITY_SCORE_WARNING = float(os.getenv("SEVERITY_SCORE_WARNING", "-0.05"))