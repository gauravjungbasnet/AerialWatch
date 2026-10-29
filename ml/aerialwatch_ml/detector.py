"""Detectors behind one interface, so the API never cares which one runs (D-006: the AI is a swappable slot).

* YoloDetector     - the trained model (Ultralytics YOLOv8, trained on BAMBI wild boar + NACAS photos).
* HotspotDetector  - a classical baseline with no training: warm blobs of pig size in a thermal image. Used when
                     no weights are configured, and reported next to YOLO in the evaluation so judges can see what
                     the learned model adds.
"""

from __future__ import annotations

import io
from dataclasses import dataclass
from pathlib import Path
from typing import Protocol

import numpy as np
from PIL import Image, ImageOps

from .schemas import Box

PIG_LABELS = {"pig", "wild_boar", "wild boar", "boar", "sus_scrofa"}


def load_image(data: bytes) -> Image.Image:
    """Decode a JPEG/PNG and apply its EXIF rotation, so pixel coordinates match what the browser shows."""
    img = Image.open(io.BytesIO(data))
    img = ImageOps.exif_transpose(img)
    return img.convert("RGB")


class Detector(Protocol):
    model_version: str
    kind: str
    classes: list[str]

    def detect(self, image: Image.Image, min_confidence: float) -> list[Box]: ...


# ---------------------------------------------------------------------------------------------------------------------
# Classical baseline
# ---------------------------------------------------------------------------------------------------------------------

@dataclass
class HotspotDetector:
    """Finds compact warm spots: robust threshold, connected components, size filter, local-contrast check.

    Sizes are fractions of the image's shorter side so the same settings work at 640 px and 4K. At ~50 m altitude an
    adult pig is roughly 1-4 % of the frame width in the study's imagery (Hvala et al. 2023).
    """

    model_version: str = "hotspot-baseline-0.1"
    kind: str = "hotspot-baseline"
    classes: tuple[str, ...] = ("pig",)
    k_sigma: float = 4.0            # how far above the background a pixel must be (robust z-score)
    min_side_frac: float = 0.008    # smallest blob side / image short side
    max_side_frac: float = 0.12     # largest blob side / image short side
    max_aspect: float = 4.0
    hot_share: float = 0.35         # threshold at least this far from the median to the hottest pixels

    def detect(self, image: Image.Image, min_confidence: float) -> list[Box]:
        import cv2  # imported here so the service can start without OpenCV when only YOLO is used

        gray = np.asarray(image.convert("L"), dtype=np.float32)
        h, w = gray.shape
        short = min(h, w)
        gray = cv2.GaussianBlur(gray, (0, 0), sigmaX=max(0.8, short / 800))

        # 1. Warm pixels: robust threshold against the whole frame (median / MAD ignore the few hot pixels).
        med = float(np.median(gray))
        mad = float(np.median(np.abs(gray - med))) * 1.4826 + 1e-6
        # Also require a share of the way to the hottest pixels, so warm vegetation on a very even background
        # (tiny MAD) doesn't pass; on a frame with no animal the k-sigma floor still applies.
        hottest = float(np.percentile(gray, 99.95))
        threshold = med + max(self.k_sigma * mad, self.hot_share * (hottest - med))
        mask = (gray > threshold).astype(np.uint8)

        # 2. Warm regions of pig size. A warm roof, rock or water body is one big region and is dropped whole.
        n, _, stats, _ = cv2.connectedComponentsWithStats(mask, connectivity=8)
        boxes: list[Box] = []
        lo, hi = self.min_side_frac * short, self.max_side_frac * short
        for i in range(1, n):
            x, y, bw, bh, area = (int(v) for v in stats[i])
            side = max(bw, bh)
            if not (lo <= side <= hi) or side / max(1, min(bw, bh)) > self.max_aspect:
                continue
            if x == 0 or y == 0 or x + bw >= w or y + bh >= h:
                continue  # cut by the frame edge: lens vignetting / stitching artefacts; the overlapping photo shows it whole

            # 3. Local contrast: the blob must stand out from a ring of ground around it (not a warm patch edge).
            pad = max(2, side)
            rx0, ry0, rx1, ry1 = max(0, x - pad), max(0, y - pad), min(w, x + bw + pad), min(h, y + bh + pad)
            ring = gray[ry0:ry1, rx0:rx1].copy()
            ring[y - ry0:y - ry0 + bh, x - rx0:x - rx0 + bw] = np.nan
            ring_level = float(np.nanmedian(ring)) if np.isfinite(ring).any() else med
            peak = float(gray[y:y + bh, x:x + bw].max())
            contrast = (peak - ring_level) / mad
            if contrast < self.k_sigma:
                continue

            fill = area / float(bw * bh)
            # "How much hotter than its surroundings, how compact": a heuristic score, not a calibrated probability.
            confidence = float(1 / (1 + np.exp(-(contrast - 2 * self.k_sigma) / self.k_sigma))) * (0.6 + 0.4 * fill)
            if confidence < min_confidence:
                continue
            m = max(1, round(0.15 * side))
            x0, y0 = max(0, x - m), max(0, y - m)
            x1, y1 = min(w, x + bw + m), min(h, y + bh + m)
            boxes.append(Box(x=x0, y=y0, width=x1 - x0, height=y1 - y0, confidence=round(confidence, 4)))
        boxes.sort(key=lambda b: b.confidence, reverse=True)
        return boxes


# ---------------------------------------------------------------------------------------------------------------------
# Trained model
# ---------------------------------------------------------------------------------------------------------------------

class YoloDetector:
    """Ultralytics YOLO weights (.pt, or .onnx for CPU servers). Only pig classes are returned."""

    kind = "yolo"

    def __init__(self, weights: Path, image_size: int, model_version: str | None = None):
        from ultralytics import YOLO  # heavy import; only when a model is configured

        self._model = YOLO(str(weights))
        self._imgsz = image_size
        names = self._model.names
        self.classes = [names[i] for i in sorted(names)] if isinstance(names, dict) else list(names)
        self._pig_ids = {i for i, n in enumerate(self.classes) if n.lower() in PIG_LABELS} or set(range(len(self.classes)))
        self.model_version = model_version or f"yolo-{weights.stem}"

    def detect(self, image: Image.Image, min_confidence: float) -> list[Box]:
        result = self._model.predict(image, imgsz=self._imgsz, conf=min_confidence, verbose=False)[0]
        boxes: list[Box] = []
        for (x0, y0, x1, y1), conf, cls in zip(result.boxes.xyxy.tolist(), result.boxes.conf.tolist(), result.boxes.cls.tolist()):
            if int(cls) not in self._pig_ids:
                continue
            boxes.append(Box(x=max(0.0, x0), y=max(0.0, y0), width=x1 - x0, height=y1 - y0, confidence=round(float(conf), 4)))
        return boxes


# ---------------------------------------------------------------------------------------------------------------------
# Tiling for large photos
# ---------------------------------------------------------------------------------------------------------------------

def tile_windows(width: int, height: int, size: int, overlap: float) -> list[tuple[int, int, int, int]]:
    """Overlapping square windows (x0, y0, x1, y1) covering the whole image; the last row/column is shifted inwards."""
    step = max(1, int(size * (1 - overlap)))

    def starts(length: int) -> list[int]:
        if length <= size:
            return [0]
        values = list(range(0, length - size, step))
        return values + [length - size]

    return [(x, y, min(width, x + size), min(height, y + size)) for y in starts(height) for x in starts(width)]


def iou(a: Box, b: Box) -> float:
    ix = max(0.0, min(a.x + a.width, b.x + b.width) - max(a.x, b.x))
    iy = max(0.0, min(a.y + a.height, b.y + b.height) - max(a.y, b.y))
    inter = ix * iy
    union = a.width * a.height + b.width * b.height - inter
    return inter / union if union > 0 else 0.0


def non_max_suppression(boxes: list[Box], threshold: float = 0.5) -> list[Box]:
    """Keeps the most confident of any boxes overlapping by more than `threshold` IoU."""
    kept: list[Box] = []
    for box in sorted(boxes, key=lambda b: b.confidence, reverse=True):
        if all(iou(box, k) <= threshold for k in kept):
            kept.append(box)
    return kept


@dataclass
class TiledDetector:
    """Runs another detector on overlapping tiles of a large photo and merges the results.

    A 20 MP RGB photo shrunk to 1024 px makes a pig a few pixels wide; on 1280 px tiles it keeps its size. Boxes that
    touch an inner tile border are dropped (the overlap guarantees the same pig appears whole in a neighbouring tile),
    then duplicates from the overlap are removed with non-maximum suppression.
    """

    inner: Detector
    tile_size: int = 1280
    overlap: float = 0.2
    edge_px: float = 2.0

    @property
    def model_version(self) -> str:
        return self.inner.model_version

    @property
    def kind(self) -> str:
        return self.inner.kind

    @property
    def classes(self) -> list[str]:
        return list(self.inner.classes)

    def detect(self, image: Image.Image, min_confidence: float) -> list[Box]:
        w, h = image.size
        if max(w, h) <= self.tile_size * 1.25:
            return self.inner.detect(image, min_confidence)

        found: list[Box] = []
        for x0, y0, x1, y1 in tile_windows(w, h, self.tile_size, self.overlap):
            for b in self.inner.detect(image.crop((x0, y0, x1, y1)), min_confidence):
                touches_inner_edge = (
                    (x0 > 0 and b.x <= self.edge_px)
                    or (y0 > 0 and b.y <= self.edge_px)
                    or (x1 < w and b.x + b.width >= (x1 - x0) - self.edge_px)
                    or (y1 < h and b.y + b.height >= (y1 - y0) - self.edge_px)
                )
                if not touches_inner_edge:
                    found.append(b.model_copy(update={"x": b.x + x0, "y": b.y + y0}))
        return non_max_suppression(found)


def create_detector(model_path: Path | None, image_size: int, model_version: str | None) -> Detector:
    """Trained YOLO if the weights exist, otherwise the hotspot baseline (thermal)."""
    if model_path and model_path.exists():
        return YoloDetector(model_path, image_size, model_version)
    return HotspotDetector()


def create_camera_detector(camera: str, model_path: Path | None, image_size: int, model_version: str | None,
                           tile: bool = False, tile_size: int = 1280, overlap: float = 0.2) -> Detector | None:
    """The detector for one camera. RGB has no training-free fallback (the hotspot idea only works on heat), so it is
    None until RGB weights are configured."""
    if model_path and model_path.exists():
        detector: Detector = YoloDetector(model_path, image_size, model_version)
    elif camera == "thermal":
        detector = HotspotDetector()
    else:
        return None
    return TiledDetector(detector, tile_size, overlap) if tile else detector
