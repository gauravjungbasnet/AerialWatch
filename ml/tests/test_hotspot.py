from aerialwatch_ml.detector import HotspotDetector
from conftest import PIGS, thermal_frame
from evaluate import iou


def test_finds_each_warm_pig_sized_blob(frame):
    boxes = HotspotDetector().detect(frame, min_confidence=0.05)
    found = [max(iou((b.x, b.y, b.width, b.height), p) for b in boxes) for p in PIGS]
    assert all(v >= 0.3 for v in found), found
    assert len(boxes) <= len(PIGS) + 1  # textured ground must not light up


def test_nothing_on_empty_ground():
    assert HotspotDetector().detect(thermal_frame(pigs=[]), min_confidence=0.05) == []


def test_large_warm_areas_are_not_pigs():
    big = thermal_frame(pigs=[(100, 100, 300, 250)])  # a warm rock or roof, ~half the frame
    assert HotspotDetector().detect(big, min_confidence=0.05) == []


def test_boxes_are_inside_the_image_and_sorted(frame):
    boxes = HotspotDetector().detect(frame, min_confidence=0.0)
    assert [b.confidence for b in boxes] == sorted((b.confidence for b in boxes), reverse=True)
    for b in boxes:
        assert b.x >= 0 and b.y >= 0 and b.x + b.width <= frame.width and b.y + b.height <= frame.height
        assert 0 <= b.confidence <= 1
