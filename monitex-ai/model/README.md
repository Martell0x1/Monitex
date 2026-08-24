# Monitex AI Service

Consumes sensor messages from the `iot.sensors.anomaly.queue` queue, scores
each reading against a model trained specifically for that
`(device_name, sensorType)` pair - on both its value *and* how fast it got
there - and publishes anomaly notifications back to RabbitMQ for the
backend to consume and relay with SignalR. A device/sensor without a
trained model yet gets a lightweight z-score fallback check instead of
going completely unmonitored.

It also retrains itself on a schedule, fully internally - no backend
involvement required. An optional RabbitMQ listener exists for an
on-demand "retrain now" trigger, but nothing needs to publish to it for
the service to work.

## Run locally

```bash
source .venv/bin/activate
set -a && source .env && set +a
python model/rabbitmq_reader_service.py
```

Runs three things concurrently until stopped (`Ctrl+C`):
anomaly detection, the internal retrain scheduler, and the optional
retrain-trigger listener.

## Manual retraining

```bash
python train_model.py
```

Pulls every `(device_name, sensorType)` series from InfluxDB over the last
`TRAIN_LOOKBACK_RANGE`, trains a separate IsolationForest per series with
enough samples, and writes `esp32_anomaly_model.joblib` as a dict keyed
`"{device_name}::{sensorType}"` (the previous file is kept as
`esp32_anomaly_model.joblib.bak`). Series without enough data are skipped
and logged, not silently merged into another sensor's model. If a series'
learned range shifts drastically from the previous retrain, a `[DRIFT
WARNING]` is logged instead of silently swapping in a possibly worse model.

## Running the tests

```bash
pip install -r requirements-dev.txt
pytest tests/ -v
```

41 tests, all synthetic (no real InfluxDB/RabbitMQ needed) - covering
per-series model isolation, the legacy-format fallback, hot reload, drift
detection, severity grading, the notification cooldown, the rate-of-change
feature, the fallback detector, and the health endpoint.

## Health endpoint

```bash
curl http://localhost:8091/health
```

(port `8091` is the host-side mapping in `docker-compose.yml`; inside the
container it's `HEALTH_CHECK_PORT`, default `8000`). Returns JSON:

```json
{
  "status": "ok",
  "known_series": ["esp32-1::ldr", "esp32-1::temperature"],
  "last_retrain_at": "2026-08-23T12:00:00+00:00",
  "last_retrain_ok": true,
  "last_drift_warnings": [],
  "readings_processed": 4231,
  "anomalies_published": 12,
  "notifications_suppressed_by_cooldown": 47,
  "uptime_seconds": 3600
}
```

## Required environment

| Variable | Purpose |
|---|---|
| `ANOMALY_MODEL_PATH` | Path to the saved `.joblib` file |
| `RABBITMQ_URL` | RabbitMQ connection string |
| `RABBITMQ_QUEUE_NAME` | Queue with incoming sensor readings |
| `RABBITMQ_EXCHANGE_NAME` | Topic exchange shared with the backend |
| `ANOMALY_RESULTS_ROUTING_KEY` | Routing key used for anomaly result messages |
| `RETRAIN_SCHEDULER_ENABLED` | `true`/`false` - internal scheduler on/off (default `true`) |
| `RETRAIN_INTERVAL_MINUTES` | How often to retrain, in minutes (default `1440` = 24h) |
| `INFLUX_URL` / `INFLUX_TOKEN` / `INFLUX_ORG` / `INFLUX_BUCKET` | Same InfluxDB the backend writes readings to |
| `TRAIN_MEASUREMENT` / `TRAIN_FIELD` | Which measurement/field to train on (device and sensor type are discovered automatically, not configured) |
| `TRAIN_LOOKBACK_RANGE` | Flux range, e.g. `-7d` |
| `TRAIN_CONTAMINATION` / `TRAIN_MAX_VALID_READING` / `TRAIN_IQR_MULTIPLIER` / `TRAIN_MIN_SAMPLES` | Training hyperparameters |
| `ANOMALY_NOTIFICATION_COOLDOWN_SECONDS` | Minimum seconds between repeat notifications for the same sensor at the same or lower severity (default `300`). An escalation (e.g. warning -> critical) always bypasses this. |
| `DRIFT_WARN_RATIO` | How much a series' learned range can shift between retrains (as a fraction of the previous range's width) before logging a `[DRIFT WARNING]` (default `0.5` = 50%) |
| `TRAIN_USE_RATE_OF_CHANGE` | `true`/`false` - whether training adds "change since the previous reading" as a second feature alongside the raw value (default `true`) |
| `FALLBACK_MIN_SAMPLES` | How many live readings a brand-new/untrained series needs before the fallback z-score check starts judging it (default `20`) |
| `FALLBACK_Z_THRESHOLD` | How many standard deviations from the running mean counts as anomalous for the fallback check (default `3.5`) |
| `HEALTH_CHECK_PORT` | Port the `/health` endpoint listens on inside the container (default `8000`) |
| `SEVERITY_SCORE_CRITICAL` / `SEVERITY_SCORE_WARNING` | IsolationForest score thresholds for grading a non-hard-bound anomaly's severity (defaults `-0.15` / `-0.05`) |

Optional (only relevant if something external wants to trigger an
on-demand retrain instead of waiting for the scheduler):

| Variable | Purpose |
|---|---|
| `RABBITMQ_RETRAIN_QUEUE_NAME` | Queue an on-demand retrain trigger would arrive on |
| `RETRAIN_ROUTING_KEY` | Routing key for that trigger |

**Never hardcode `INFLUX_TOKEN` (or any secret) in source.** Set it via the
environment / docker-compose / a local `.env` that is gitignored.

## Architecture notes

- **One model per sensor series.** Training discovers every
  `(device_name, sensorType)` tag pair in InfluxDB and fits a separate
  `IsolationForest` + `StandardScaler` + learned range per series. At
  inference time, a reading is only ever scored against the model trained
  on *that* device+sensor - never a different one. If no model exists yet
  for a series (not enough historical data), the reading is logged and
  skipped rather than guessed at with someone else's threshold.
- **Self-contained retraining.** `_run_scheduled_retrain` is an internal
  `asyncio` loop - no external trigger needed. `train_and_save_model()` is
  shared between the CLI script, the scheduler, and the optional
  RabbitMQ-triggered path, and an `asyncio.Lock` prevents two retrains
  from overlapping if both happen to fire close together.
- **Hot reload.** `model_runtime.py` holds the live models behind a lock.
  `reload()` loads and normalizes the new bundle fully before swapping it
  in, so predictions in flight during a retrain always see a consistent
  set of models.
- **Backward compatible.** A legacy single-model `.joblib` (the old
  `(model, scaler, profile)` tuple format) is automatically treated as a
  fallback `"__default__"` model, so nothing breaks before the first retrain
  under the new format runs.
- **Severity grading uses the model's own confidence, not just hard
  bounds.** A reading beyond the learned IQR range but still within the
  wider "ever seen" ceiling gets graded `info`/`warning`/`critical` by how
  negative the IsolationForest's `decision_function` score is, instead of
  every non-hard-bound anomaly collapsing into one generic "warning".
- **Notification cooldown.** A sensor stuck out of range no longer floods
  the frontend with one SignalR notification per reading. Repeat
  notifications for the same `(device, sensorType)` are throttled by
  `ANOMALY_NOTIFICATION_COOLDOWN_SECONDS`, except an escalation in severity
  always gets through immediately regardless of cooldown.
- **Drift detection.** Every retrain compares each series' newly learned
  range against its previous one; a shift larger than `DRIFT_WARN_RATIO`
  logs a loud `[DRIFT WARNING]` instead of silently accepting a possibly
  worse model.
- **Rate-of-change feature.** A value staying within its learned normal
  range can still be anomalous because of *how it got there* - e.g. a
  light sensor jumping from 25 to 39 in one reading looks identical,
  value-wise, to it sitting steadily at 39, but the sudden jump is often
  the actual signal something's wrong. When `TRAIN_USE_RATE_OF_CHANGE` is
  on (default), training adds "change since the previous retained
  reading" as a second feature, and `model_runtime.observe()` tracks the
  last raw value per series live so the same feature can be computed at
  inference time. Legacy/old profiles without this flag are unaffected -
  they keep scoring on the raw value alone.
- **Fallback detector for brand-new series.** A device/sensor that hasn't
  accumulated enough data for the last retrain would otherwise go
  completely unmonitored until the next one runs (`fallback_detector.py`).
  A lightweight Welford's-algorithm running mean/std check kicks in once
  `FALLBACK_MIN_SAMPLES` live readings have been seen, flagging anything
  more than `FALLBACK_Z_THRESHOLD` standard deviations away as a
  provisional (lower-confidence) anomaly - not a replacement for the real
  per-series models, just enough to close the gap until one exists.
- **Health endpoint.** `health_server.py` runs a minimal stdlib-only HTTP
  server on a background thread, exposing current status, known trained
  series, live counters (readings processed, anomalies published,
  notifications suppressed by cooldown), and the last retrain's
  outcome/drift warnings as JSON - so "is this actually working" doesn't
  require grepping container logs.