"""Detection service (FastAPI). Internal only: the .NET API calls it; browsers never do (AGENTS.md).

    uvicorn aerialwatch_ml.service:app --port 8000

POST /detect?camera=thermal|rgb   multipart "file" (JPEG/PNG) -> boxes in original pixels + model version
GET  /model?camera=thermal|rgb                                 -> which detector is loaded for that camera
GET  /models                                                   -> both cameras
GET  /health                                                   -> {"status": "ok"}

Thermal without weights uses the hotspot baseline; RGB without weights answers 503 (no training-free RGB detector).
"""

from __future__ import annotations

import time
from contextlib import asynccontextmanager
from typing import Literal

from fastapi import FastAPI, File, HTTPException, Query, UploadFile
from PIL import UnidentifiedImageError

from .config import CAMERAS, load_settings
from .detector import Detector, create_camera_detector, load_image
from .schemas import DetectResponse, ModelInfo

Camera = Literal["thermal", "rgb"]

settings = load_settings()
state: dict[str, Detector | None] = {}


@asynccontextmanager
async def lifespan(_: FastAPI):
    for cam in CAMERAS:
        m = settings.models[cam]
        state[cam] = create_camera_detector(cam, m.path, m.image_size, m.version, m.tile, settings.tile_size, settings.tile_overlap)
    yield
    state.clear()


app = FastAPI(title="Aerial Watch detection service", version="0.2.0", lifespan=lifespan)


def detector(camera: str) -> Detector:
    d = state.get(camera)
    if d is None:
        raise HTTPException(503, f"No {camera} detector is configured. Set AW_MODEL_PATH_{camera.upper()} to trained weights.")
    return d


def info(camera: str) -> ModelInfo:
    d = state.get(camera)
    m = settings.models[camera]
    if d is None:
        return ModelInfo(camera=camera, available=False, min_confidence=settings.min_confidence,
                         notes=f"No {camera} model yet: rangers review {camera} photos manually.")
    return ModelInfo(
        camera=camera,
        available=True,
        model_version=d.model_version,
        kind=d.kind,
        classes=list(d.classes),
        min_confidence=settings.min_confidence,
        image_size=m.image_size if d.kind == "yolo" else None,
        tiled=m.tile,
        notes="Hotspot baseline: no training, thermal photos only." if d.kind != "yolo" else "Trained detector.",
    )


@app.get("/health")
def health() -> dict[str, str]:
    return {"status": "ok"}


@app.get("/model", response_model=ModelInfo)
def model(camera: Camera = "thermal") -> ModelInfo:
    return info(camera)


@app.get("/models", response_model=list[ModelInfo])
def models() -> list[ModelInfo]:
    return [info(cam) for cam in CAMERAS]


@app.post("/detect", response_model=DetectResponse)
async def detect(
    file: UploadFile = File(...),
    camera: Camera = Query("thermal"),
    min_confidence: float | None = Query(None, ge=0, le=1),
) -> DetectResponse:
    d = detector(camera)
    data = await file.read(settings.max_upload_bytes + 1)
    if len(data) > settings.max_upload_bytes:
        raise HTTPException(413, "Photo is larger than the service accepts.")
    if not data:
        raise HTTPException(400, "Empty upload.")
    try:
        image = load_image(data)
    except (UnidentifiedImageError, OSError):
        raise HTTPException(400, "Not a readable JPEG or PNG image.")

    started = time.perf_counter()
    boxes = d.detect(image, settings.min_confidence if min_confidence is None else min_confidence)
    return DetectResponse(
        camera=camera,
        model_version=d.model_version,
        image_width=image.width,
        image_height=image.height,
        boxes=boxes,
        elapsed_ms=round((time.perf_counter() - started) * 1000, 1),
    )
