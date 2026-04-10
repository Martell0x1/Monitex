import os
from pathlib import Path


RABBITMQ_URL = os.getenv("RABBITMQ_URL", "amqp://guest:guest@localhost:5672/")
RABBITMQ_QUEUE_NAME = os.getenv("RABBITMQ_QUEUE_NAME", "iot.sensors.anomaly.queue")
RABBITMQ_EXCHANGE_NAME = os.getenv("RABBITMQ_EXCHANGE_NAME", "iot.sensors.exchange")
ANOMALY_RESULTS_ROUTING_KEY = os.getenv(
    "ANOMALY_RESULTS_ROUTING_KEY",
    "sensor.anomaly.detected",
)
MODEL_PATH = os.getenv(
    "ANOMALY_MODEL_PATH",
    str(Path(__file__).resolve().parent.parent / "esp32_anomaly_model.joblib"),
)
