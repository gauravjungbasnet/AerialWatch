"""Convert BAMBI flights (video + MOT-style annotations) into a YOLO dataset of wild-boar ("pig") boxes.

BAMBI (github.com/bambi-eco/Dataset, CC BY 4.0 for the data we use) ships one video per flight, thermal on the left
half and RGB on the right half (1024 x 1024 each), and one annotation file per flight with columns

    frame, track_id, bb_left, bb_top, bb_width, bb_height, conf, class_id, visibility, species, gender, age, is_propagated

Wild boar is Wikidata Q58697 -> class_id 60-69 (sex/age/visibility variants). We keep those (and hybrid pigs, by
species name) as our single class "pig"; every other animal is left unlabelled, so deer become hard negatives.

Flights are split into train/val/test *by flight*, never by frame: neighbouring frames are near-identical, and a
frame-level split would leak test images into training and inflate the metrics.

Choose flights first, from annotations only (no video):

    python tools/bambi_to_yolo.py --bambi-root ../../data/bambi/annotations --stats ../../evidence/<date>-ml/flight-stats.csv \
        --bambi-splits ../../data/bambi/Dataset/flight_metadata/splits.json --metadata ../../data/bambi/Dataset/flight_metadata

Usage (after downloading flights with BAMBI's download_from_zenodo.py):

    python tools/bambi_to_yolo.py --bambi-root ../../data/bambi --out ../../data/yolo-bambi --stride 15 --check 20

Then open data/yolo-bambi/check/*.jpg and make sure the boxes sit on the pigs. If they are shifted, adjust
--frame-offset (0- vs 1-based frames) or --x-offset (annotations in full-frame instead of thermal-view pixels).
"""

from __future__ import annotations

import argparse
import csv
import json
import random
import re
from collections import defaultdict
from dataclasses import dataclass, field
from pathlib import Path

PIG_CLASS_IDS = set(range(60, 70))
PIG_SPECIES = {"sus scrofa", "wild boar", "hybrid pig"}  # substrings; also catches "Sus scrofa × Sus domesticus"
VIEW_SIZE = 1024


@dataclass
class Row:
    frame: int
    track: int
    x: float
    y: float
    w: float
    h: float
    class_id: int | None
    species: str
    propagated: bool


@dataclass
class Flight:
    flight_id: str
    gt_path: Path
    video_path: Path
    pig_rows: dict[int, list[Row]] = field(default_factory=lambda: defaultdict(list))
    annotated_frames: set[int] = field(default_factory=set)


def parse_range(text: str) -> set[int]:
    ids: set[int] = set()
    for part in filter(None, (p.strip() for p in text.split(","))):
        if "-" in part:
            a, b = part.split("-")
            ids.update(range(int(a), int(b) + 1))
        else:
            ids.add(int(part))
    return ids


def read_rows(gt_path: Path) -> list[Row]:
    rows: list[Row] = []
    with gt_path.open(newline="", encoding="utf-8") as f:
        for rec in csv.reader(f):
            if not rec or rec[0].strip().lower() == "frame":
                continue
            rec = [c.strip() for c in rec]
            try:
                class_id = int(float(rec[7])) if len(rec) > 7 and rec[7] else None
            except ValueError:
                class_id = None
            rows.append(Row(
                frame=int(float(rec[0])), track=int(float(rec[1])),
                x=float(rec[2]), y=float(rec[3]), w=float(rec[4]), h=float(rec[5]),
                class_id=class_id,
                species=rec[9].lower() if len(rec) > 9 else "",
                propagated=len(rec) > 12 and rec[12] == "1",
            ))
    return rows


def is_pig(row: Row, class_ids: set[int], species: set[str]) -> bool:
    # BAMBI writes names like "Sus scrofa (Wild boar)", so match by substring, as their filter_flights.py does.
    return (row.class_id is not None and row.class_id in class_ids) or any(s in row.species for s in species)


def find_flights(root: Path, wanted: set[str] | None, view: str = "thermal") -> list[Flight]:
    """Thermal uses <flight>_gt.txt (boxes drawn on the thermal view). RGB uses <flight>_rgb_gt.txt, BAMBI's
    "owl-transferred" boxes for the RGB view (download with --version owl-transferred); the thermal boxes don't line up
    with the RGB view, so flights without RGB boxes are skipped for RGB."""
    suffix = "_rgb_gt.txt" if view == "rgb" else "_gt.txt"
    flights = []
    for gt in sorted(root.rglob(f"*{suffix}")):
        if view != "rgb" and gt.name.endswith("_rgb_gt.txt"):
            continue
        flight_id = gt.name[: -len(suffix)]
        if wanted and flight_id not in wanted:
            continue
        videos = sorted(gt.parent.glob(f"{flight_id}_*.mp4")) or sorted(gt.parent.glob(f"{flight_id}*.mp4"))
        if not videos:
            print(f"! {flight_id}: annotations but no video next to {gt.name}; skipped")
            continue
        flights.append(Flight(flight_id, gt, videos[0]))
    if view == "rgb" and wanted:
        missing = sorted(wanted - {f.flight_id for f in flights})
        if missing:
            print(f"! no RGB boxes (<id>_rgb_gt.txt) or video for: {', '.join(missing)}")
    return flights


def split_flights(flights: list[Flight], ratios: tuple[float, float, float], seed: int,
                  official: Path | None = None) -> dict[str, list[Flight]]:
    """BAMBI's own flight-level split (flight_metadata/splits.json) when given, so our test scores are comparable
    with other work on BAMBI; otherwise a seeded random split by flight. Flights missing from the file go to train."""
    if official and official.exists():
        table = json.loads(official.read_text())
        where = {str(f): split for split, ids in table.items() for f in ids}
        out: dict[str, list[Flight]] = {"train": [], "val": [], "test": []}
        for fl in flights:
            out[where.get(fl.flight_id, "train")].append(fl)
        return out
    order = flights[:]
    random.Random(seed).shuffle(order)
    n = len(order)
    n_test = max(1, round(n * ratios[2])) if n >= 3 else 0
    n_val = max(1, round(n * ratios[1])) if n >= 3 else (1 if n == 2 else 0)
    return {"test": order[:n_test], "val": order[n_test:n_test + n_val], "train": order[n_test + n_val:]}


def to_yolo_line(r: Row, x_offset: float) -> str | None:
    """Clip to the 1024 x 1024 view and convert to normalised centre/size. None if nothing is left."""
    x0 = max(0.0, r.x - x_offset)
    y0 = max(0.0, r.y)
    x1 = min(VIEW_SIZE, r.x - x_offset + r.w)
    y1 = min(VIEW_SIZE, r.y + r.h)
    if x1 - x0 < 2 or y1 - y0 < 2:
        return None
    cx, cy = (x0 + x1) / 2 / VIEW_SIZE, (y0 + y1) / 2 / VIEW_SIZE
    return f"0 {cx:.6f} {cy:.6f} {(x1 - x0) / VIEW_SIZE:.6f} {(y1 - y0) / VIEW_SIZE:.6f}"


def choose_frames(flight: Flight, stride: int, keyframes_only: bool, negatives: float, seed: int) -> tuple[list[int], list[int]]:
    """Every `stride`-th frame that has a pig, plus a share of frames without pigs (hard negatives)."""
    pig_frames = sorted(f for f, rows in flight.pig_rows.items() if not keyframes_only or any(not r.propagated for r in rows))
    positives = pig_frames[::stride]
    others = sorted(flight.annotated_frames - set(flight.pig_rows))[::stride]
    rng = random.Random(f"{seed}-{flight.flight_id}")
    k = min(len(others), round(len(positives) * negatives)) if positives else min(len(others), 20)
    return positives, sorted(rng.sample(others, k))


def extract(flight: Flight, frames: set[int], view: str, frame_offset: int):
    """Yields (frame_number, image) for the wanted frames, reading the video once from start to end."""
    import cv2

    cap = cv2.VideoCapture(str(flight.video_path))
    if not cap.isOpened():
        raise RuntimeError(f"Cannot open {flight.video_path}")
    index, last = 0, max(frames) if frames else -1
    try:
        while index - frame_offset <= last:
            ok, img = cap.read()
            if not ok:
                break
            number = index - frame_offset  # annotation frame number for this video frame
            if number in frames:
                h, w = img.shape[:2]
                half = w // 2 if w >= 2 * h - 8 else w   # side-by-side video -> two views
                yield number, (img[:, :half] if view == "thermal" else img[:, half:])
            index += 1
    finally:
        cap.release()


def write_stats(args) -> None:
    """Per-flight pig counts from annotation files only, ranked by hand-annotated pig keyframes."""
    class_ids = parse_range(args.class_ids)
    species = {s.strip().lower() for s in args.species.split(",") if s.strip()}
    official: dict[str, str] = {}
    if args.bambi_splits and args.bambi_splits.exists():
        official = {str(f): split for split, ids in json.loads(args.bambi_splits.read_text()).items() for f in ids}

    rows = []
    for gt in sorted(args.bambi_root.rglob("*_gt.txt")):
        if gt.name.endswith("_rgb_gt.txt"):
            continue
        flight_id = gt.name[: -len("_gt.txt")]
        data = read_rows(gt)
        pigs = [r for r in data if is_pig(r, class_ids, species)]
        if not pigs:
            continue
        others = sorted({r.species for r in data if not is_pig(r, class_ids, species) and r.species})
        row = {
            "flight": flight_id,
            "split": official.get(flight_id, ""),
            "pig_boxes": len(pigs),
            "pig_frames": len({r.frame for r in pigs}),
            "pig_keyframes": len({r.frame for r in pigs if not r.propagated}),
            "pig_tracks": len({r.track for r in pigs}),
            "frames_kept_at_stride": len({r.frame for r in pigs}) // max(1, args.stride),
            "other_species": "; ".join(others),
            "has_video": any(gt.parent.glob(f"{flight_id}*.mp4")),
            "date": "", "drone": "", "weather": "",
        }
        meta = args.metadata / f"{flight_id}_metadata.json" if args.metadata else None
        if meta and meta.exists():
            info = json.loads(meta.read_text()).get("flight_info", {})
            row.update(date=(info.get("start_time") or "")[:10], drone=info.get("drone_name") or "", weather=info.get("weather", ""))
        rows.append(row)

    rows.sort(key=lambda r: (-r["pig_keyframes"], -r["pig_boxes"]))
    args.stats.parent.mkdir(parents=True, exist_ok=True)
    with args.stats.open("w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=list(rows[0]) if rows else ["flight"])
        w.writeheader()
        w.writerows(rows)
    print(f"{len(rows)} flights with pigs -> {args.stats}")
    for split in ("train", "val", "test", ""):
        part = [r for r in rows if r["split"] == split]
        if part:
            print(f"  {split or 'no split':8} {len(part):3} flights, {sum(r['pig_boxes'] for r in part):7} pig boxes, "
                  f"~{sum(r['frames_kept_at_stride'] for r in part):5} frames at stride {args.stride}")


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--bambi-root", type=Path, required=True, help="Folder with <flight>_gt.txt and <flight>_*.mp4 files")
    ap.add_argument("--out", type=Path, help="Output dataset folder (required unless --stats)")
    ap.add_argument("--stats", type=Path, default=None,
                    help="Only read the annotation files (no video needed) and write per-flight pig counts to this CSV, "
                         "to choose which flights to download")
    ap.add_argument("--metadata", type=Path, default=None,
                    help="BAMBI flight_metadata folder; adds date, drone, weather and species to --stats")
    ap.add_argument("--flights", default="", help="Comma-separated flight ids (default: every flight found)")
    ap.add_argument("--class-ids", default="60-69", help="BAMBI class_ids counted as pig (default wild boar 60-69)")
    ap.add_argument("--species", default=",".join(sorted(PIG_SPECIES)), help="Species names counted as pig")
    ap.add_argument("--view", choices=["thermal", "rgb"], default="thermal",
                    help="rgb = right half of the video with <flight>_rgb_gt.txt boxes (BAMBI owl-transferred)")
    ap.add_argument("--stride", type=int, default=15, help="Keep every Nth frame (video is ~30 fps; neighbours are near-duplicates)")
    ap.add_argument("--keyframes-only", action="store_true", help="Only hand-annotated frames (no interpolated boxes)")
    ap.add_argument("--negatives", type=float, default=0.15, help="Frames without pigs, as a share of frames with pigs")
    ap.add_argument("--split", default="0.7,0.15,0.15", help="train,val,test shares (by flight); ignored with --bambi-splits")
    ap.add_argument("--bambi-splits", type=Path, default=None,
                    help="Use BAMBI's official split file, e.g. Dataset/flight_metadata/splits.json (recommended)")
    ap.add_argument("--frame-offset", type=int, default=0, help="1 if annotation frames are 1-based")
    ap.add_argument("--x-offset", type=float, default=0.0, help="Subtract from bb_left (e.g. 1024 for RGB-half coordinates)")
    ap.add_argument("--check", type=int, default=0, help="Also write N preview images with boxes drawn, to check alignment")
    ap.add_argument("--seed", type=int, default=2026)
    args = ap.parse_args()
    if args.stats:
        write_stats(args)
        return
    if args.out is None:
        ap.error("--out is required (or use --stats)")

    import cv2

    class_ids = parse_range(args.class_ids)
    species = {s.strip().lower() for s in args.species.split(",") if s.strip()}
    wanted = {f.strip() for f in args.flights.split(",") if f.strip()} or None

    flights = find_flights(args.bambi_root, wanted, args.view)
    if not flights:
        raise SystemExit(f"No <flight>_gt.txt + video pairs under {args.bambi_root}")

    for fl in flights:
        for r in read_rows(fl.gt_path):
            fl.annotated_frames.add(r.frame)
            if is_pig(r, class_ids, species):
                fl.pig_rows[r.frame].append(r)
    with_pigs = [f for f in flights if f.pig_rows]
    print(f"{len(flights)} flights found, {len(with_pigs)} with pigs")

    ratios = tuple(float(v) for v in args.split.split(","))
    splits = split_flights(with_pigs, ratios, args.seed, args.bambi_splits)  # type: ignore[arg-type]
    manifest: list[dict] = []
    checks_left = args.check
    safe = re.compile(r"[^A-Za-z0-9_-]")

    for split, members in splits.items():
        (args.out / "images" / split).mkdir(parents=True, exist_ok=True)
        (args.out / "labels" / split).mkdir(parents=True, exist_ok=True)
        for fl in members:
            positives, negatives = choose_frames(fl, args.stride, args.keyframes_only, args.negatives, args.seed)
            wanted_frames = set(positives) | set(negatives)
            written = 0
            for number, img in extract(fl, wanted_frames, args.view, args.frame_offset):
                stem = f"{safe.sub('_', fl.flight_id)}_{number:06d}"
                lines = [l for l in (to_yolo_line(r, args.x_offset) for r in fl.pig_rows.get(number, [])) if l]
                cv2.imwrite(str(args.out / "images" / split / f"{stem}.jpg"), img, [cv2.IMWRITE_JPEG_QUALITY, 95])
                (args.out / "labels" / split / f"{stem}.txt").write_text("\n".join(lines) + ("\n" if lines else ""))
                manifest.append({"split": split, "flight": fl.flight_id, "frame": number, "image": f"images/{split}/{stem}.jpg", "pigs": len(lines)})
                written += 1
                if checks_left > 0 and lines:
                    (args.out / "check").mkdir(exist_ok=True)
                    preview = img.copy()
                    for r in fl.pig_rows[number]:
                        x = int(r.x - args.x_offset)
                        cv2.rectangle(preview, (x, int(r.y)), (x + int(r.w), int(r.y + r.h)), (0, 255, 0), 2)
                    cv2.imwrite(str(args.out / "check" / f"{stem}.jpg"), preview)
                    checks_left -= 1
            print(f"  {split:5} {fl.flight_id}: {written} frames ({len(positives)} with pigs, {len(negatives)} without)")

    names = ["pig"]
    (args.out / "data.yaml").write_text(
        f"# Generated by tools/bambi_to_yolo.py. Source: BAMBI dataset (CC BY 4.0), wild boar only.\n"
        f"path: {args.out.resolve().as_posix()}\ntrain: images/train\nval: images/val\ntest: images/test\n"
        f"names:\n" + "".join(f"  {i}: {n}\n" for i, n in enumerate(names))
    )
    with (args.out / "manifest.csv").open("w", newline="") as f:
        w = csv.DictWriter(f, fieldnames=["split", "flight", "frame", "image", "pigs"])
        w.writeheader()
        w.writerows(manifest)

    stats = {
        split: {
            "flights": sorted({m["flight"] for m in manifest if m["split"] == split}),
            "images": sum(1 for m in manifest if m["split"] == split),
            "images_with_pigs": sum(1 for m in manifest if m["split"] == split and m["pigs"]),
            "boxes": sum(m["pigs"] for m in manifest if m["split"] == split),
        }
        for split in ("train", "val", "test")
    }
    stats["settings"] = {k: (str(v) if isinstance(v, Path) else v) for k, v in vars(args).items()}
    (args.out / "dataset-card.json").write_text(json.dumps(stats, indent=2))
    print(json.dumps({k: {kk: vv for kk, vv in v.items() if kk != "flights"} for k, v in stats.items() if k != "settings"}, indent=2))


if __name__ == "__main__":
    main()
