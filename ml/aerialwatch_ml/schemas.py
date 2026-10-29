"""Wire format between the .NET API and the detection service. Boxes are in original-image pixels, top-left origin,
the same convention as wfp.Detections (D-034), so the API can insert them without conversion."""

from pydantic import BaseModel, Field


class Box(BaseModel):
    x: float = Field(ge=0)
    y: float = Field(ge=0)
    width: float = Field(gt=0)
    height: float = Field(gt=0)
    label: str = "pig"
    confidence: float = Field(ge=0, le=1)


class DetectResponse(BaseModel):
    camera: str
    model_version: str
    image_width: int
    image_height: int
    boxes: list[Box]
    elapsed_ms: float


class ModelInfo(BaseModel):
    camera: str
    available: bool
    model_version: str | None = None
    kind: str | None = None  # "yolo", "hotspot-baseline" or None (no detector for this camera)
    classes: list[str] = []
    min_confidence: float
    image_size: int | None = None
    tiled: bool = False
    notes: str
