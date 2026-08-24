import threading
from typing import Any

import joblib
import pandas as pd

DEFAULT_KEY = "__default__"


def _model_key(device_name: str | None, sensor_type: str | None) -> str | None:
    if not device_name or not sensor_type:
        return None
    return f"{device_name}::{sensor_type}"


class AnomalyModelRuntime:

    def __init__(self, model_path: str) -> None:
        self._lock = threading.Lock()
        self.model_path = model_path
        self.models: dict[str, tuple] = {}
        self._last_value: dict[str, float] = {}
        self._load(model_path)

    @staticmethod
    def _normalize_bundle(model_bundle: Any) -> dict[str, tuple]:
        if isinstance(model_bundle, dict):
            return model_bundle

        if isinstance(model_bundle, tuple) and len(model_bundle) == 3:
            return {DEFAULT_KEY: model_bundle}

        if isinstance(model_bundle, tuple) and len(model_bundle) == 2:
            model, scaler = model_bundle
            return {DEFAULT_KEY: (model, scaler, None)}

        raise ValueError("Unsupported model bundle format in joblib file")

    def _load(self, model_path: str) -> None:
        model_bundle = joblib.load(model_path)
        self.models = self._normalize_bundle(model_bundle)

    def reload(self, model_path: str | None = None) -> None:
        
        path = model_path or self.model_path
        model_bundle = joblib.load(path)
        models = self._normalize_bundle(model_bundle)

        with self._lock:
            self.model_path = path
            self.models = models

    def _lookup(self, device_name: str | None, sensor_type: str | None):
        with self._lock:
            models = self.models

        key = _model_key(device_name, sensor_type)
        if key is not None and key in models:
            return models[key]

        return models.get(DEFAULT_KEY)

    def observe(
        self,
        value: float,
        device_name: str | None = None,
        sensor_type: str | None = None,
    ) -> float:

        key = _model_key(device_name, sensor_type) or DEFAULT_KEY

        with self._lock:
            previous = self._last_value.get(key)
            self._last_value[key] = value

        return 0.0 if previous is None else value - previous

    @staticmethod
    def _build_features(entry: tuple, value: float, delta: float | None) -> pd.DataFrame:
        _model, _scaler, training_profile = entry
        uses_delta = bool(training_profile and training_profile.get("uses_rate_of_change"))

        if uses_delta:
            return pd.DataFrame([[value, delta or 0.0]], columns=["_value", "_delta"])

        return pd.DataFrame([[value]], columns=["_value"])

    def predict(
        self,
        value: float,
        device_name: str | None = None,
        sensor_type: str | None = None,
        delta: float | None = None,
    ) -> int | None:


        entry = self._lookup(device_name, sensor_type)
        if entry is None:
            return None

        model, scaler, training_profile = entry

        if training_profile is not None:
            lower_bound = float(training_profile["lower_bound"])
            upper_bound = float(training_profile["upper_bound"])

            if value < lower_bound or value > upper_bound:
                return -1

        features = scaler.transform(self._build_features(entry, value, delta))
        return int(model.predict(features)[0])

    def score(
        self,
        value: float,
        device_name: str | None = None,
        sensor_type: str | None = None,
        delta: float | None = None,
    ) -> float | None:

        entry = self._lookup(device_name, sensor_type)
        if entry is None:
            return None

        model, scaler, _training_profile = entry
        features = scaler.transform(self._build_features(entry, value, delta))
        return float(model.decision_function(features)[0])

    def upper_threshold(self, device_name: str | None = None, sensor_type: str | None = None) -> float | None:
        entry = self._lookup(device_name, sensor_type)
        if entry is None or entry[2] is None:
            return None
        return float(entry[2]["upper_bound"])

    def max_valid_reading(self, device_name: str | None = None, sensor_type: str | None = None) -> float | None:
        entry = self._lookup(device_name, sensor_type)
        if entry is None or entry[2] is None:
            return None
        return float(entry[2]["max_valid_reading"])

    def trained_at(self, device_name: str | None = None, sensor_type: str | None = None) -> str | None:
        entry = self._lookup(device_name, sensor_type)
        if entry is None or entry[2] is None:
            return None
        return entry[2].get("trained_at")

    def known_series(self) -> list[str]:
        with self._lock:
            return sorted(k for k in self.models.keys() if k != DEFAULT_KEY)

    def as_dict(self, device_name: str | None = None, sensor_type: str | None = None) -> dict[str, Any]:
        return {
            "upper_threshold": self.upper_threshold(device_name, sensor_type),
            "max_valid_reading": self.max_valid_reading(device_name, sensor_type),
            "trained_at": self.trained_at(device_name, sensor_type),
        }