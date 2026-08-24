# Monitex AI - Accuracy Evaluation

Generated: 2026-08-24T19:41:50.799818+00:00
Data source: Live InfluxDB history

Anomaly detection here is unsupervised (IsolationForest trained on unlabeled sensor readings), so there's no ground-truth accuracy to read off training. This report measures precision/recall against **anomalies injected into held-out real data** - values the model never saw during training, with a known correct answer.

| Series | Train samples | Holdout FPR | Spike recall | Sudden-jump sensitivity |
|---|---:|---:|---:|---:|
| `esp32-1::ldr` | 237 | 5.0% | 100.0% | 3x |

*Sudden-jump sensitivity: the smallest one-step change (as a multiple of this sensor's normal step-to-step variation, measured exactly as the model itself measures it) that gets caught at least 80% of the time - still within the learned normal value range, only catchable via the rate-of-change feature. Lower is more sensitive.*

## Per-series jump detection curve

- `esp32-1::ldr`: 2x=0.0%, 3x=100.0%, 4x=100.0%, 5x=95.0%, 6x=100.0%, 8x=100.0%, 10x=100.0%, 15x=n/a (too large to fit in range), 20x=n/a (too large to fit in range)

## Overall (all series combined)

- **False positive rate on genuine normal data:** 5.0%
- **Precision:** 98.0%
- **Recall (spikes + largest tested jump size):** 87.6%
- **F1 score:** 92.5%

*A low false-positive rate close to the configured `TRAIN_CONTAMINATION` (default 1%) means the model isn't crying wolf on normal data. High spike recall means hard out-of-range anomalies always get caught. The jump detection curve shows how sudden a change has to be, relative to this sensor's normal behavior, before the rate-of-change feature flags it.*