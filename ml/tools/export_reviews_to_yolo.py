"""Export photos reviewed in Aerial Watch to a YOLO dataset (D-038), for local fine-tuning on NACAS photos.

Runs ONLY on the developer's PC. NACAS photos and the exported dataset must never leave it (docs/11).

    pip install pyodbc
    python tools/export_reviews_to_yolo.py --survey-prefix "NACAS" --out C:\\NACAS-data\\yolo-nacas ^
        --val 130221_1000 --test 140221

What it exports
  * Surveys whose name starts with --survey-prefix (one survey per NACAS flight folder).
  * Only photos marked checked in the review tool (Reviewed = 1). Flagged photos are skipped unless --keep-flagged.
  * Boxes with Status = Accepted (ranger-drawn or AI-accepted) and Label = pig. Rejected/pending boxes are ignored.
  * Checked photos with no pigs are kept with an empty label file (they teach the model "nothing here").
Split is BY SURVEY (flight), never by photo: neighbouring photos overlap, so a photo split would inflate the scores.
  --test / --val take comma-separated substrings of survey names; everything else is train.
"""

from __future__ import annotations

import argparse
import json
import shutil
from collections import Counter
from pathlib import Path

DEFAULT_CONN = "Server=.;Database=AerialWatch;Trusted_Connection=yes;TrustServerCertificate=yes"
SQL = """
SELECT s.Name AS Survey, i.Id AS ImageId, i.FileName, i.StoragePath, i.WidthPx, i.HeightPx, i.Flagged,
       d.X, d.Y, d.Width, d.Height
FROM wfp.Surveys s
JOIN wfp.SurveyImages i ON i.SurveyId = s.Id AND i.Reviewed = 1
LEFT JOIN wfp.Detections d ON d.ImageId = i.Id AND d.Status = 'Accepted' AND d.Label = 'pig'
WHERE s.Name LIKE ?
ORDER BY s.Name, i.FileName
"""


def connect(conn: str):
    import pyodbc
    drivers = [d for d in ("ODBC Driver 18 for SQL Server", "ODBC Driver 17 for SQL Server") if d in pyodbc.drivers()]
    if not drivers:
        raise SystemExit("No SQL Server ODBC driver found. Install 'ODBC Driver 18 for SQL Server' from Microsoft.")
    return pyodbc.connect(f"Driver={{{drivers[0]}}};{conn}")


def split_of(survey: str, val: list[str], test: list[str]) -> str:
    if any(t in survey for t in test):
        return "test"
    if any(v in survey for v in val):
        return "val"
    return "train"


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--survey-prefix", required=True, help="Export surveys whose name starts with this, e.g. NACAS")
    ap.add_argument("--out", type=Path, required=True, help="Output folder; keep it outside the repo, e.g. C:\\NACAS-data\\yolo-nacas")
    ap.add_argument("--storage-root", type=Path, default=Path(r"C:\AerialWatchData"), help="Storage:RootPath of the API")
    ap.add_argument("--conn", default=DEFAULT_CONN, help="SQL connection (same database as appsettings.json)")
    ap.add_argument("--val", default="", help="Comma-separated substrings of survey names for validation")
    ap.add_argument("--test", default="", help="Comma-separated substrings of survey names for the held-out test set")
    ap.add_argument("--keep-flagged", action="store_true", help="Also export photos flagged as unsure")
    args = ap.parse_args()

    val = [v.strip() for v in args.val.split(",") if v.strip()]
    test = [t.strip() for t in args.test.split(",") if t.strip()]
    repo = Path(__file__).resolve().parents[3]
    if repo in args.out.resolve().parents:
        raise SystemExit(f"--out is inside the repo ({repo}). NACAS data must stay outside it (docs/11).")

    rows = connect(args.conn).cursor().execute(SQL, f"{args.survey_prefix}%").fetchall()
    if not rows:
        raise SystemExit("No checked photos found. Mark photos checked in the review tool (Space) first.")

    images: dict[str, dict] = {}
    for r in rows:
        img = images.setdefault(str(r.ImageId), {
            "survey": r.Survey, "file": r.FileName, "path": r.StoragePath, "w": r.WidthPx, "h": r.HeightPx,
            "flagged": bool(r.Flagged), "boxes": []})
        if r.X is not None:
            img["boxes"].append((r.X, r.Y, r.Width, r.Height))

    if args.out.exists():
        shutil.rmtree(args.out)
    stats: Counter = Counter()
    surveys: dict[str, str] = {}
    for image_id, img in images.items():
        if img["flagged"] and not args.keep_flagged:
            stats["skipped_flagged"] += 1
            continue
        split = split_of(img["survey"], val, test)
        surveys[img["survey"]] = split
        src = args.storage_root / Path(img["path"])
        if not src.exists():
            stats["missing_file"] += 1
            print(f"! missing {src}")
            continue
        w, h = img["w"], img["h"]
        if not w or not h:
            from PIL import Image
            with Image.open(src) as im:
                w, h = im.size
        stem = f"{img['survey']}_{Path(img['file']).stem}".replace(" ", "_")
        (args.out / "images" / split).mkdir(parents=True, exist_ok=True)
        (args.out / "labels" / split).mkdir(parents=True, exist_ok=True)
        shutil.copy2(src, args.out / "images" / split / f"{stem}{src.suffix.lower()}")
        lines = []
        for x, y, bw, bh in img["boxes"]:
            x0, y0 = max(0.0, x), max(0.0, y)
            x1, y1 = min(float(w), x + bw), min(float(h), y + bh)
            if x1 - x0 < 1 or y1 - y0 < 1:
                continue
            lines.append(f"0 {(x0 + x1) / 2 / w:.6f} {(y0 + y1) / 2 / h:.6f} {(x1 - x0) / w:.6f} {(y1 - y0) / h:.6f}")
        (args.out / "labels" / split / f"{stem}.txt").write_text("\n".join(lines) + ("\n" if lines else ""))
        stats[f"{split}_images"] += 1
        stats[f"{split}_pigs"] += len(lines)
        stats[f"{split}_empty"] += 0 if lines else 1

    for split in ("train", "val", "test"):
        (args.out / "images" / split).mkdir(parents=True, exist_ok=True)
        (args.out / "labels" / split).mkdir(parents=True, exist_ok=True)
    (args.out / "data.yaml").write_text(
        f"path: {args.out.resolve().as_posix()}\ntrain: images/train\nval: images/val\ntest: images/test\n"
        "names:\n  0: pig\n")
    card = {"source": "NACAS (CDU), local only, do not share (docs/11)", "survey_prefix": args.survey_prefix,
            "surveys": dict(sorted(surveys.items())), "counts": dict(sorted(stats.items()))}
    (args.out / "dataset-card.json").write_text(json.dumps(card, indent=2))
    print(json.dumps(card, indent=2))
    if not stats["val_images"] or not stats["test_images"]:
        print("! val or test is empty: check --val/--test match your survey names")


if __name__ == "__main__":
    main()
