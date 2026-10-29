from PIL import Image, ImageDraw

from aerialwatch_ml.detector import TiledDetector, non_max_suppression, tile_windows
from aerialwatch_ml.schemas import Box


class WhiteBlobDetector:
    """Fake inner detector: one box around all pure-white pixels of the crop it is given."""

    model_version = "fake-1"
    kind = "yolo"
    classes = ["pig"]

    def __init__(self):
        self.calls = 0

    def detect(self, image, min_confidence):
        self.calls += 1
        bbox = image.convert("L").point(lambda v: 255 if v > 200 else 0).getbbox()
        if bbox is None:
            return []
        x0, y0, x1, y1 = bbox
        return [Box(x=x0, y=y0, width=x1 - x0, height=y1 - y0, confidence=0.9)]


def big_photo(pig=(1500, 1000, 40, 30), size=(4000, 3000)) -> Image.Image:
    img = Image.new("RGB", size, (60, 60, 60))
    x, y, w, h = pig
    ImageDraw.Draw(img).rectangle([x, y, x + w - 1, y + h - 1], fill=(255, 255, 255))
    return img


def test_windows_cover_the_whole_image_with_overlap():
    windows = tile_windows(4000, 3000, 1280, 0.2)
    assert windows[0] == (0, 0, 1280, 1280)
    assert max(x1 for _, _, x1, _ in windows) == 4000 and max(y1 for _, _, _, y1 in windows) == 3000
    assert all(x1 - x0 == 1280 and y1 - y0 == 1280 for x0, y0, x1, y1 in windows)


def test_small_photo_is_not_tiled():
    inner = WhiteBlobDetector()
    TiledDetector(inner).detect(big_photo(pig=(100, 100, 20, 20), size=(1024, 768)), 0.25)
    assert inner.calls == 1


def test_pig_seen_in_several_tiles_is_returned_once_in_photo_coordinates():
    inner = WhiteBlobDetector()
    boxes = TiledDetector(inner, tile_size=1280, overlap=0.2).detect(big_photo(), 0.25)
    assert inner.calls > 1
    assert len(boxes) == 1
    b = boxes[0]
    assert (b.x, b.y, b.width, b.height) == (1500, 1000, 40, 30)


def test_nms_keeps_the_most_confident_of_overlapping_boxes():
    a = Box(x=0, y=0, width=10, height=10, confidence=0.9)
    b = Box(x=1, y=1, width=10, height=10, confidence=0.5)
    c = Box(x=50, y=50, width=10, height=10, confidence=0.4)
    assert non_max_suppression([b, c, a]) == [a, c]
