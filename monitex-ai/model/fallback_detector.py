import math


class _RunningStats:

    __slots__ = ("count", "mean", "m2")

    def __init__(self) -> None:
        self.count = 0
        self.mean = 0.0
        self.m2 = 0.0

    def update(self, value: float) -> None:
        self.count += 1
        delta = value - self.mean
        self.mean += delta / self.count
        delta2 = value - self.mean
        self.m2 += delta * delta2

    def std(self) -> float:
        if self.count < 2:
            return 0.0
        return math.sqrt(self.m2 / (self.count - 1))


class FallbackAnomalyDetector:

    def __init__(self, min_samples: int = 20, z_threshold: float = 3.5) -> None:
        self._min_samples = min_samples
        self._z_threshold = z_threshold
        self._stats: dict[str, _RunningStats] = {}

    def observe(self, key: str, value: float) -> tuple[bool, float | None]:

        stats = self._stats.setdefault(key, _RunningStats())

        is_anomaly = False
        z_score = None

        if stats.count >= self._min_samples:
            std = stats.std()
            if std > 0:
                z_score = (value - stats.mean) / std
                is_anomaly = abs(z_score) > self._z_threshold

        stats.update(value)
        return is_anomaly, z_score

    def known_keys(self) -> list[str]:
        return sorted(self._stats.keys())