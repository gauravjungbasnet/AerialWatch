import io
import sys
from pathlib import Path

import numpy as np
import pytest
from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / "tools"))

PIGS = [(100, 120, 22, 14), (400, 300, 26, 16), (520, 80, 20, 12)]  # x, y, w, h


def thermal_frame(pigs=PIGS, size=(640, 512), seed=1) -> Image.Image:
    """Synthetic thermal frame: textured cool ground with warm, pig-sized ellipses."""
    rng = np.random.default_rng(seed)
    w, h = size
    yy, xx = np.mgrid[0:h, 0:w]
    ground = 60 + 15 * np.sin(xx / 37) * np.cos(yy / 53) + rng.normal(0, 3, (h, w))
    img = Image.fromarray(np.clip(ground, 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(img)
    for x, y, bw, bh in pigs:
        d.ellipse([x, y, x + bw, y + bh], fill=220)
    return img.filter(ImageFilter.GaussianBlur(1.2)).convert("RGB")


def jpeg_bytes(img: Image.Image) -> bytes:
    buf = io.BytesIO()
    img.save(buf, "JPEG", quality=92)
    return buf.getvalue()


@pytest.fixture
def frame() -> Image.Image:
    return thermal_frame()
