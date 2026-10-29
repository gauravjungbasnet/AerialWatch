"""Evaluate a detector on a YOLO-format test split and write metrics for the evidence folder.

    python tools/evaluate.py --data ../../data/yolo-bambi --split test --weights models/yolov8s-bambi.pt --out ../../evidence/<date>-ml
    python tools/evaluate.py --data ../../data/yolo-bambi --split test --baseline --out ...      # hotspot baseline

Reports, at IoU >= 0.5 (a box counts as found if it overlaps a real pig by at least half):
  * precision  - of the boxes the AI drew, how many were pigs        (low = rangers reject a lot)
  * recall     - of the real pigs, how many the AI found             (low = rangers draw a lot)
  * F1, AP50   - the usual single-number summaries
  * count error per image - what matters for the estimate before review
at the service's confidence threshold, plus the threshold with the best F1 (a suggestion for AW_MIN_CONFIDENCE).
"""

from __future__ import annotations

import argparse
import json
import sys
from dataclasses import dataclass
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[1]))

Box = tuple[float, float, float, float]  # x, y, w, h in pixels


def iou(a: Box, b: Box) -> float:
    ax1, ay1, bx1, by1 = a[0] + a[2], a[1] + a[3], b[0] + b[2], b[1] + b[3]
    iw = max(0.0, min(ax1, bx1) - max(a[0], b[0]))
    ih = max(0.0, min(ay1, by1) - max(a[1], b[1]))
    inter = iw * ih
    union = a[2] * a[3] + b[2] * b[3] - inter
    return inter / union if union > 0 else 0.0


def match(preds: list[tuple[Box, float]], truths: list[Box], threshold: float = 0.5) -> list[tuple[float, bool]]:
    """Greedy matching, most confident prediction first (the COCO/VOC rule). Returns (confidence, is_true_positive)."""
    used: set[int] = set()
    out: list[tuple[float, bool]] = []
    for box, conf in sorted(preds, key=lambda p: -p[1]):
        best, best_i = 0.0, -1
        for i, t in enumerate(truths):
            if i in used:
                continue
            v = iou(box, t)
            if v > best:
                best, best_i = v, i
        hit = best >= threshold
        if hit:
            used.add(best_i)
        out.append((conf, hit))
    return out


@dataclass
class Metrics:
    threshold: float
    tp: int
    fp: int
    fn: int

    @property
    def precision(self) -> float:
        return self.tp / (self.tp + self.fp) if self.tp + self.fp else 0.0

    @property
    def recall(self) -> float:
        return self.tp / (self.tp + self.fn) if self.tp + self.fn else 0.0

    @property
    def f1(self) -> float:
        p, r = self.precision, self.recall
        return 2 * p * r / (p + r) if p + r else 0.0


def metrics_at(scored: list[tuple[float, bool]], n_truth: int, threshold: float) -> Metrics:
    kept = [hit for conf, hit in scored if conf >= threshold]
    tp = sum(kept)
    return Metrics(threshold, tp, len(kept) - tp, n_truth - tp)


def average_precision(scored: list[tuple[float, bool]], n_truth: int) -> float:
    """Area under the precision-recall curve with all-point interpolation (VOC 2010+)."""
    if n_truth == 0:
        return 0.0
    tp = fp = 0
    points = [(0.0, 1.0)]
    for _, hit in sorted(scored, key=lambda s: -s[0]):
        tp += hit
        fp += not hit
        points.append((tp / n_truth, tp / (tp + fp)))
    ap, prev_r = 0.0, 0.0
    for i, (r, _) in enumerate(points):
        p_interp = max(p for _, p in points[i:])
        ap += (r - prev_r) * p_interp
        prev_r = r
    return ap


def read_labels(path: Path, w: int, h: int) -> list[Box]:
    if not path.exists():
        return []
    boxes = []
    for line in path.read_text().split("\n"):
        parts = line.split()
        if len(parts) == 5:
            _, cx, cy, bw, bh = map(float, parts)
            boxes.append(((cx - bw / 2) * w, (cy - bh / 2) * h, bw * w, bh * h))
    return boxes


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", type=Path, required=True, help="YOLO dataset folder (with images/<split> and labels/<split>)")
    ap.add_argument("--split", default="test")
    g = ap.add_mutually_exclusive_group(required=True)
    g.add_argument("--weights", type=Path)
    g.add_argument("--baseline", action="store_true", help="Evaluate the hotspot baseline instead of a trained model")
    ap.add_argument("--imgsz", type=int, default=1024)
    ap.add_argument("--threshold", type=float, default=0.25, help="Confidence threshold the service uses (AW_MIN_CONFIDENCE)")
    ap.add_argument("--out", type=Path, required=True)
    ap.add_argument("--tile", action="store_true", help="Tile large photos like the service does for RGB (AW_TILE_RGB)")
    ap.add_argument("--tile-size", type=int, default=1280)
    args = ap.parse_args()

    from PIL import Image

    from aerialwatch_ml.detector import HotspotDetector, TiledDetector, YoloDetector

    detector = HotspotDetector() if args.baseline else YoloDetector(args.weights, args.imgsz)
    if args.tile:
        detector = TiledDetector(detector, args.tile_size)
    folder = args.data / "images" / args.split
    images = sorted(p for p in folder.glob("*") if p.suffix.lower() in {".jpg", ".jpeg", ".png"})
    if not images:
        raise SystemExit(f"No images in {args.data / 'images' / args.split}")

    scored: list[tuple[float, bool]] = []
    n_truth = 0
    count_errors: list[int] = []
    for path in images:
        img = Image.open(path).convert("RGB")
        truths = read_labels(args.data / "labels" / args.split / (path.stem + ".txt"), img.width, img.height)
        preds = [((b.x, b.y, b.width, b.height), b.confidence) for b in detector.detect(img, min_confidence=0.01)]
        scored += match(preds, truths)
        n_truth += len(truths)
        count_errors.append(sum(c >= args.threshold for _, c in preds) - len(truths))

    at_service = metrics_at(scored, n_truth, args.threshold)
    best = max((metrics_at(scored, n_truth, t / 100) for t in range(5, 96)), key=lambda m: m.f1)
    ap50 = average_precision(scored, n_truth)
    mae = sum(abs(e) for e in count_errors) / len(count_errors)
    bias = sum(count_errors) / len(count_errors)

    def row(m: Metrics) -> dict:
        return {"threshold": m.threshold, "precision": round(m.precision, 4), "recall": round(m.recall, 4), "f1": round(m.f1, 4), "tp": m.tp, "fp": m.fp, "fn": m.fn}

    result = {
        "detector": detector.model_version,
        "kind": detector.kind,
        "dataset": str(args.data),
        "split": args.split,
        "images": len(images),
        "pigs": n_truth,
        "iou_threshold": 0.5,
        "ap50": round(ap50, 4),
        "at_service_threshold": row(at_service),
        "best_f1": row(best),
        "count_error_per_image": {"mean_absolute": round(mae, 3), "mean_signed": round(bias, 3)},
    }
    args.out.mkdir(parents=True, exist_ok=True)
    stem = f"metrics-{detector.model_version}-{args.split}"
    (args.out / f"{stem}.json").write_text(json.dumps(result, indent=2))
    (args.out / f"{stem}.md").write_text(
        f"# Detector metrics: {detector.model_version} ({args.split} split)\n\n"
        f"{len(images)} images, {n_truth} pigs, IoU >= 0.5. Test flights were never seen in training (split by flight).\n\n"
        "| Threshold | Precision | Recall | F1 | Found | False boxes | Missed |\n|---|---|---|---|---|---|---|\n"
        + "".join(f"| {m.threshold:.2f}{' (service)' if m is at_service else ' (best F1)'} | {m.precision:.1%} | {m.recall:.1%} | {m.f1:.3f} | {m.tp} | {m.fp} | {m.fn} |\n" for m in (at_service, best))
        + f"\nAP50: **{ap50:.3f}**. Count error per image at the service threshold: mean absolute {mae:.2f}, mean signed {bias:+.2f} "
        "(positive = AI over-counts before review).\n\n"
        "These numbers describe the AI's suggestions only. Every box is checked by a ranger before it is counted (D-034), "
        "and the population estimate uses the research model separately (D-006).\n"
    )
    print(json.dumps(result, indent=2))


if __name__ == "__main__":
    main()
