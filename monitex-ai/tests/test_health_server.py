import json
import urllib.request

from health_server import HealthStatus, start_health_server


def test_snapshot_includes_uptime_and_defaults():
    status = HealthStatus()
    snapshot = status.snapshot()

    assert snapshot["status"] == "starting"
    assert snapshot["known_series"] == []
    assert snapshot["last_retrain_at"] is None
    assert "uptime_seconds" in snapshot
    assert snapshot["uptime_seconds"] >= 0


def test_update_merges_fields_without_clobbering_others():
    status = HealthStatus()
    status.update(status="ok", known_series=["esp32-1::ldr"])

    snapshot = status.snapshot()
    assert snapshot["status"] == "ok"
    assert snapshot["known_series"] == ["esp32-1::ldr"]
    assert snapshot["last_retrain_at"] is None


def test_health_endpoint_serves_json_over_real_http():
    status = HealthStatus()
    status.update(status="ok", known_series=["esp32-1::ldr", "esp32-1::temperature"])

    server = start_health_server(status, port=0)
    port = server.server_address[1]

    try:
        with urllib.request.urlopen(f"http://127.0.0.1:{port}/health", timeout=2) as response:
            assert response.status == 200
            assert response.headers["Content-Type"] == "application/json"
            body = json.loads(response.read().decode("utf-8"))

        assert body["status"] == "ok"
        assert body["known_series"] == ["esp32-1::ldr", "esp32-1::temperature"]
        assert "uptime_seconds" in body
    finally:
        server.shutdown()


def test_unknown_path_returns_404():
    status = HealthStatus()
    server = start_health_server(status, port=0)
    port = server.server_address[1]

    try:
        try:
            urllib.request.urlopen(f"http://127.0.0.1:{port}/not-a-real-path", timeout=2)
            assert False, "expected an HTTPError for an unknown path"
        except urllib.error.HTTPError as exc:
            assert exc.code == 404
    finally:
        server.shutdown()