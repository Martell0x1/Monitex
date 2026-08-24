

from __future__ import annotations

import argparse
import os
import sys
import tempfile
from datetime import datetime, timezone
from pathlib import Path

import joblib
import numpy as np
import pandas as pd

sys.path.insert(0, str(Path(__file__).resolve().parent / "model"))

import train_model  # noqa: E402
from model_runtime import AnomalyModelRuntime  # noqa: E402
from config import TRAIN_MIN_SAMPLES  # noqa: E402


def split_train_holdout(df: pd.DataFrame, train_frac: float = 0.8) -> tuple[pd.DataFrame, pd.DataFrame]:
    n_train = int(len(df) * train_frac)
    return df.iloc[:n_train].copy(), df.iloc[n_train:].copy()


def evaluate_series(key: str, train_df: pd.DataFrame, holdout_df: pd.DataFrame) -> dict | None:
    fitted = train_model.fit_series(train_df)
    if fitted is None:
        return None

    model, scaler, profile = fitted
    device_name, sensor_type = key.split("::", 1)

    tmp_path = tempfile.NamedTemporaryFile(suffix=".joblib", delete=False).name
    joblib.dump({key: (model, scaler, profile)}, tmp_path)

    try:
      
        runtime = AnomalyModelRuntime(tmp_path)
        false_positives = 0
        for value in holdout_df["_value"]:
            delta = runtime.observe(value, device_name, sensor_type)
            if runtime.predict(value, device_name, sensor_type, delta=delta) == -1:
                false_positives += 1
        holdout_n = len(holdout_df)
        false_positive_rate = false_positives / holdout_n if holdout_n else None

        
        sample_values = holdout_df["_value"].sample(
            min(30, holdout_n), random_state=1
        ).tolist()

        spike_hits = 0
        for value in sample_values:
            spike_value = value + (profile["upper_bound"] - profile["lower_bound"]) * 5
            r = AnomalyModelRuntime(tmp_path)
            d = r.observe(spike_value, device_name, sensor_type)
            if r.predict(spike_value, device_name, sensor_type, delta=d) == -1:
                spike_hits += 1
        spike_recall = spike_hits / len(sample_values) if sample_values else None

       
        uses_rate_of_change = bool(profile.get("uses_rate_of_change"))

        range_width = profile["upper_bound"] - profile["lower_bound"]
        midpoint = (profile["upper_bound"] + profile["lower_bound"]) / 2

        recall_by_multiplier: dict[float, float | None] = {}
        min_reliable_multiplier: float | None = None
        jump_hits = 0
        jump_trials = 0

        if not uses_rate_of_change:
           
            recall_by_multiplier = {}
        else:
            delta_std = float(scaler.scale_[1])
            if delta_std <= 0:
                delta_std = 1.0

            jump_multipliers = [2, 3, 4, 5, 6, 8, 10, 15, 20]
            trials_per_multiplier = min(20, holdout_n)
            reliable_threshold = 0.8  # this fraction of trials must be caught

            rng = np.random.default_rng(123)

            for multiplier in jump_multipliers:
                jump_size = delta_std * multiplier

             
                if jump_size >= range_width * 0.9:
                    recall_by_multiplier[multiplier] = None
                    continue

                hits = 0
                for _ in range(trials_per_multiplier):
                    jitter = rng.uniform(-0.02, 0.02) * range_width
                    base_value = midpoint - jump_size / 2 + jitter
                    jumped_value = base_value + jump_size

                    r = AnomalyModelRuntime(tmp_path)
                    r.observe(base_value, device_name, sensor_type)
                    d = r.observe(jumped_value, device_name, sensor_type)
                    if r.predict(jumped_value, device_name, sensor_type, delta=d) == -1:
                        hits += 1

                recall = hits / trials_per_multiplier if trials_per_multiplier else 0.0
                recall_by_multiplier[multiplier] = recall
                jump_hits += hits
                jump_trials += trials_per_multiplier

                if min_reliable_multiplier is None and recall >= reliable_threshold:
                    min_reliable_multiplier = multiplier

        jump_recall = (jump_hits / jump_trials) if jump_trials else None
    finally:
        os.remove(tmp_path)

    return {
        "key": key,
        "train_samples": len(train_df),
        "holdout_samples": holdout_n,
        "false_positives": false_positives,
        "false_positive_rate": false_positive_rate,
        "spike_hits": spike_hits,
        "spike_total": len(sample_values),
        "spike_recall": spike_recall,
        "jump_hits": jump_hits,
        "jump_total": jump_trials,
        "jump_recall": jump_recall,
        "recall_by_multiplier": recall_by_multiplier,
        "min_reliable_multiplier": min_reliable_multiplier,
        "uses_rate_of_change": uses_rate_of_change,
    }


def format_pct(value: float | None) -> str:
    return f"{value:.1%}" if value is not None else "n/a"


def format_multiplier(value: float | None) -> str:
    return f"{value:.0f}x" if value is not None else "not detected in tested range"


def build_report(results: list[dict]) -> str:
    lines = [
        "# Monitex AI - Accuracy Evaluation",
        "",
        f"Generated: {datetime.now(timezone.utc).isoformat()}",
        "Data source: Live InfluxDB history",
        "",
        "Anomaly detection here is unsupervised (IsolationForest trained on "
        "unlabeled sensor readings), so there's no ground-truth accuracy to "
        "read off training. This report measures precision/recall against "
        "**anomalies injected into held-out real data** - values the model "
        "never saw during training, with a known correct answer.",
        "",
        "| Series | Train samples | Holdout FPR | Spike recall | Sudden-jump sensitivity |",
        "|---|---:|---:|---:|---:|",
    ]

    total_fp = 0
    total_holdout = 0
    total_tp = 0
    total_positives = 0

    for r in results:
        sensitivity_label = (
            format_multiplier(r["min_reliable_multiplier"])
            if r["uses_rate_of_change"]
            else "n/a (rate-of-change disabled for this series)"
        )
        lines.append(
            f"| `{r['key']}` | {r['train_samples']} | "
            f"{format_pct(r['false_positive_rate'])} | "
            f"{format_pct(r['spike_recall'])} | "
            f"{sensitivity_label} |"
        )
        total_fp += r["false_positives"]
        total_holdout += r["holdout_samples"]
        total_tp += r["spike_hits"] + r["jump_hits"]
        total_positives += r["spike_total"] + r["jump_total"]

    precision = total_tp / (total_tp + total_fp) if (total_tp + total_fp) else None
    recall = total_tp / total_positives if total_positives else None
    f1 = (
        2 * precision * recall / (precision + recall)
        if precision and recall and (precision + recall) > 0
        else None
    )

    lines += [
        "",
        "*Sudden-jump sensitivity: the smallest one-step change (as a "
        "multiple of this sensor's normal step-to-step variation, "
        "measured exactly as the model itself measures it) that gets "
        "caught at least 80% of the time - still within the learned "
        "normal value range, only catchable via the rate-of-change "
        "feature. Lower is more sensitive.*",
        "",
        "## Per-series jump detection curve",
        "",
    ]

    for r in results:
        if not r["uses_rate_of_change"]:
            lines.append(f"- `{r['key']}`: rate-of-change was disabled for this series' training")
            continue

        sweep_parts = []
        for mult, rec in r["recall_by_multiplier"].items():
            if rec is None:
                sweep_parts.append(f"{mult:.0f}x=n/a (too large to fit in range)")
            else:
                sweep_parts.append(f"{mult:.0f}x={format_pct(rec)}")
        lines.append(f"- `{r['key']}`: {', '.join(sweep_parts)}")

    lines += [
        "",
        "## Overall (all series combined)",
        "",
        f"- **False positive rate on genuine normal data:** {format_pct(total_fp / total_holdout if total_holdout else None)}",
        f"- **Precision:** {format_pct(precision)}",
        f"- **Recall (spikes + largest tested jump size):** {format_pct(recall)}",
        f"- **F1 score:** {format_pct(f1)}",
        "",
        "*A low false-positive rate close to the configured "
        "`TRAIN_CONTAMINATION` (default 1%) means the model isn't crying "
        "wolf on normal data. High spike recall means hard out-of-range "
        "anomalies always get caught. The jump detection curve shows how "
        "sudden a change has to be, relative to this sensor's normal "
        "behavior, before the rate-of-change feature flags it.*",
    ]

    return "\n".join(lines)


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--report",
        type=str,
        default="LIVE_EVALUATION_REPORT.md",
        help="Path to write the markdown report to (default: LIVE_EVALUATION_REPORT.md)",
    )
    args = parser.parse_args()

    df = train_model.load_all_sensor_data()

    if df.empty:
        print("No data available to evaluate - check InfluxDB has history for the configured lookback range.")
        return

    results = []
    for (device_name, sensor_type), group in df.groupby(["device_name", "sensorType"]):
        key = f"{device_name}::{sensor_type}"

        if len(group) < TRAIN_MIN_SAMPLES * 2:
            print(f"Skipping {key}: not enough real data yet for a train/holdout split.")
            continue

        train_df, holdout_df = split_train_holdout(group)
        result = evaluate_series(key, train_df, holdout_df)
        if result is None:
            print(f"Skipping {key}: no samples left after filtering.")
            continue

        results.append(result)
        print(
            f"{key}: FPR={format_pct(result['false_positive_rate'])} "
            f"spike_recall={format_pct(result['spike_recall'])} "
            f"jump_recall={format_pct(result['jump_recall'])}"
        )

    if not results:
        print("No series had enough real data to evaluate.")
        return

    report = build_report(results)
    print("\n" + report)

    with open(args.report, "w") as f:
        f.write(report)
    print(f"\nReport written to {args.report}")


if __name__ == "__main__":
    main()