import json
import threading
import time
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer


class HealthStatus:
  

    def __init__(self) -> None:
        self._lock = threading.Lock()
        self._started_at = time.time()
        self._data: dict = {
            "status": "starting",
            "known_series": [],
            "last_retrain_at": None,
            "last_retrain_ok": None,
            "last_drift_warnings": [],
            "readings_processed": 0,
            "anomalies_published": 0,
            "notifications_suppressed_by_cooldown": 0,
        }

    def update(self, **fields) -> None:
        with self._lock:
            self._data.update(fields)

    def increment(self, field: str, by: int = 1) -> None:
        with self._lock:
            self._data[field] = self._data.get(field, 0) + by

    def snapshot(self) -> dict:
        with self._lock:
            data = dict(self._data)
        data["uptime_seconds"] = round(time.time() - self._started_at)
        return data


def start_health_server(status: HealthStatus, port: int) -> ThreadingHTTPServer:

    class Handler(BaseHTTPRequestHandler):
        def do_GET(self) -> None:
            if self.path != "/health":
                self.send_response(404)
                self.end_headers()
                return

            body = json.dumps(status.snapshot()).encode("utf-8")
            self.send_response(200)
            self.send_header("Content-Type", "application/json")
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        def log_message(self, format_str: str, *args) -> None:
        
            pass

    server = ThreadingHTTPServer(("0.0.0.0", port), Handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    return server