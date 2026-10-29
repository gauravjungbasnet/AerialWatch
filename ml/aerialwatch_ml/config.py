"""Settings from environment variables (so the same container runs on a laptop, a VM or Azure).

One detector per camera, because thermal and RGB photos need separate models (D-035, docs/16):

    AW_MODEL_PATH_THERMAL / AW_MODEL_VERSION_THERMAL   trained thermal weights (AW_MODEL_PATH / AW_MODEL_VERSION also work)
    AW_MODEL_PATH_RGB     / AW_MODEL_VERSION_RGB       trained RGB weights (no RGB model -> /detect?camera=rgb answers 503)
    AW_IMAGE_SIZE_THERMAL, AW_IMAGE_SIZE_RGB           inference size, must match training (default 1024)
    AW_TILE_THERMAL (default false), AW_TILE_RGB (default true)   cut large photos into tiles first
    AW_TILE_SIZE (default 1280), AW_TILE_OVERLAP (default 0.2)
    AW_MIN_CONFIDENCE (default 0.25), AW_MAX_UPLOAD_MB (default 50)
"""

import os
from dataclasses import dataclass
from pathlib import Path

CAMERAS = ("thermal", "rgb")


@dataclass(frozen=True)
class CameraModel:
    # Trained YOLO weights (.pt or .onnx). Missing: thermal falls back to the hotspot baseline, RGB has no detector.
    path: Path | None
    # Name recorded on every box in wfp.Detections.ModelVersion, e.g. "yolov8s-bambi-2026-10-28".
    version: str | None
    # Inference size; must match training (tools/train.py --imgsz).
    image_size: int
    # Cut photos much larger than the tile into overlapping tiles (keeps small pigs visible in 20 MP RGB photos).
    tile: bool


@dataclass(frozen=True)
class Settings:
    models: dict[str, CameraModel]
    # Boxes below this confidence are not returned (the ranger never sees them).
    min_confidence: float
    tile_size: int
    tile_overlap: float
    # Largest upload accepted, in bytes.
    max_upload_bytes: int


def _env(*names: str, default: str = "") -> str:
    for name in names:
        value = os.getenv(name, "").strip()
        if value:
            return value
    return default


def _flag(name: str, default: bool) -> bool:
    return _env(name, default="true" if default else "false").lower() in {"1", "true", "yes", "on"}


def load_settings() -> Settings:
    def camera(cam: str, legacy: bool, tile_default: bool) -> CameraModel:
        up = cam.upper()
        path = _env(f"AW_MODEL_PATH_{up}", *(["AW_MODEL_PATH"] if legacy else []))
        return CameraModel(
            path=Path(path) if path else None,
            version=_env(f"AW_MODEL_VERSION_{up}", *(["AW_MODEL_VERSION"] if legacy else [])) or None,
            image_size=int(_env(f"AW_IMAGE_SIZE_{up}", *(["AW_IMAGE_SIZE"] if legacy else []), default="1024")),
            tile=_flag(f"AW_TILE_{up}", tile_default),
        )

    return Settings(
        models={"thermal": camera("thermal", legacy=True, tile_default=False), "rgb": camera("rgb", legacy=False, tile_default=True)},
        min_confidence=float(_env("AW_MIN_CONFIDENCE", default="0.25")),
        tile_size=int(_env("AW_TILE_SIZE", default="1280")),
        tile_overlap=float(_env("AW_TILE_OVERLAP", default="0.2")),
        max_upload_bytes=int(_env("AW_MAX_UPLOAD_MB", default="50")) * 1024 * 1024,
    )
