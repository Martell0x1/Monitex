# RabbitMQ Reader Service

Consumes sensor messages from the `iot.sensors.anomaly.queue` queue, loads the trained `.joblib` anomaly model, scores incoming readings, and publishes anomaly notifications back to RabbitMQ for the backend to consume and relay with SignalR.

## Run

```bash
source .venv/bin/activate
set -a && source .env && set +a
python model/rabbitmq_reader_service.py
```

The service listens continuously until stopped (`Ctrl+C`).

## Required Environment

- `ANOMALY_MODEL_PATH`: path to the saved `.joblib` file
- `RABBITMQ_EXCHANGE_NAME`: RabbitMQ topic exchange shared with the backend
- `ANOMALY_RESULTS_ROUTING_KEY`: routing key used for anomaly result messages
