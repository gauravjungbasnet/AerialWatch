from fastapi.testclient import TestClient

from aerialwatch_ml.service import app
from conftest import jpeg_bytes


def client() -> TestClient:
    return TestClient(app)


def test_health_and_model_info():
    with client() as c:
        assert c.get("/health").json() == {"status": "ok"}
        info = c.get("/model").json()
        assert info["camera"] == "thermal" and info["available"]
        assert info["kind"] == "hotspot-baseline"  # no weights configured in tests
        assert info["model_version"] == "hotspot-baseline-0.1"


def test_detect_returns_boxes_in_original_pixels(frame):
    with client() as c:
        r = c.post("/detect", files={"file": ("DJI_0001.JPG", jpeg_bytes(frame), "image/jpeg")}, params={"min_confidence": 0.05})
    assert r.status_code == 200
    body = r.json()
    assert (body["image_width"], body["image_height"]) == (640, 512)
    assert body["camera"] == "thermal"
    assert body["model_version"] == "hotspot-baseline-0.1"
    assert len(body["boxes"]) >= 3
    assert set(body["boxes"][0]) == {"x", "y", "width", "height", "label", "confidence"}
    assert body["boxes"][0]["label"] == "pig"


def test_rejects_files_that_are_not_images():
    with client() as c:
        r = c.post("/detect", files={"file": ("notes.txt", b"hello", "text/plain")})
    assert r.status_code == 400


def test_rejects_empty_upload():
    with client() as c:
        r = c.post("/detect", files={"file": ("a.jpg", b"", "image/jpeg")})
    assert r.status_code == 400


def test_min_confidence_must_be_a_probability(frame):
    with client() as c:
        r = c.post("/detect", files={"file": ("a.jpg", jpeg_bytes(frame), "image/jpeg")}, params={"min_confidence": 2})
    assert r.status_code == 422


def test_rgb_without_a_trained_model_is_unavailable(frame):
    with client() as c:
        models = {m["camera"]: m for m in c.get("/models").json()}
        assert models["thermal"]["available"] and not models["rgb"]["available"]
        r = c.post("/detect", files={"file": ("a.jpg", jpeg_bytes(frame), "image/jpeg")}, params={"camera": "rgb"})
    assert r.status_code == 503
    assert "AW_MODEL_PATH_RGB" in r.json()["detail"]


def test_unknown_camera_is_rejected(frame):
    with client() as c:
        r = c.post("/detect", files={"file": ("a.jpg", jpeg_bytes(frame), "image/jpeg")}, params={"camera": "lidar"})
    assert r.status_code == 422
