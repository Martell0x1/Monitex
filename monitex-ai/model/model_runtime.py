from typing import Any

import joblib
import pandas as pd


class AnomalyModelRuntime:
    def __init__(self, model_path: str) -> None:
        model_bundle = joblib.load(model_path)

        if isinstance(model_bundle, tuple) and len(model_bundle) == 3:
            self.model, self.scaler, self.training_profile = model_bundle
        elif isinstance(model_bundle, tuple) and len(model_bundle) == 2:
            self.model, self.scaler = model_bundle
            self.training_profile = None
        else:
            raise ValueError("Unsupported model bundle format in joblib file")

    def predict(self, value: float) -> int:
        if self.training_profile is not None:
            lower_bound = float(self.training_profile["lower_bound"])
            upper_bound = float(self.training_profile["upper_bound"])

            if value < lower_bound or value > upper_bound:
                return -1

        features = self.scaler.transform(pd.DataFrame([[value]], columns=["_value"]))
        return int(self.model.predict(features)[0])

    def upper_threshold(self) -> float | None:
        if not self.training_profile:
            return None
        return float(self.training_profile["upper_bound"])

    def max_valid_reading(self) -> float | None:
        if not self.training_profile:
            return None
        return float(self.training_profile["max_valid_reading"])

    def as_dict(self) -> dict[str, Any]:
        return {
            "upper_threshold": self.upper_threshold(),
            "max_valid_reading": self.max_valid_reading(),
        }
