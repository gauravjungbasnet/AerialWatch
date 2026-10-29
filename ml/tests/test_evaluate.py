import pytest

from evaluate import average_precision, iou, match, metrics_at


def test_iou():
    assert iou((0, 0, 10, 10), (0, 0, 10, 10)) == 1
    assert iou((0, 0, 10, 10), (20, 20, 5, 5)) == 0
    assert iou((0, 0, 10, 10), (5, 0, 10, 10)) == pytest.approx(50 / 150)


def test_each_pig_is_matched_once_most_confident_first():
    truths = [(0, 0, 10, 10)]
    scored = match([((0, 0, 10, 10), 0.6), ((1, 1, 10, 10), 0.9)], truths)
    assert scored == [(0.9, True), (0.6, False)]  # the duplicate is a false box


def test_metrics_and_ap():
    truths = [(0, 0, 10, 10), (50, 50, 10, 10)]
    scored = match([((0, 0, 10, 10), 0.9), ((100, 100, 10, 10), 0.8), ((50, 50, 10, 10), 0.3)], truths)
    m = metrics_at(scored, 2, 0.5)
    assert (m.tp, m.fp, m.fn) == (1, 1, 1)
    assert m.precision == 0.5 and m.recall == 0.5
    # PR points: (0.5, 1.0), (0.5, 0.5), (1.0, 0.667) -> AP = 0.5*1 + 0.5*0.667
    assert average_precision(scored, 2) == pytest.approx(0.5 + 0.5 * 2 / 3)
