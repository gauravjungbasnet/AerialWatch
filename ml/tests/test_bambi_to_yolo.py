"""End-to-end check of the converter on a tiny synthetic BAMBI-style flight (side-by-side thermal|RGB video + gt)."""

import subprocess
import sys
from pathlib import Path

import cv2
import numpy as np

TOOLS = Path(__file__).resolve().parents[1] / "tools"


def make_flight(root: Path, flight: str, frames: int = 30) -> None:
    root.mkdir(parents=True, exist_ok=True)
    video = cv2.VideoWriter(str(root / f"{flight}_matched_processed.mp4"), cv2.VideoWriter_fourcc(*"mp4v"), 30, (2048, 1024))
    rows = []
    for f in range(frames):
        img = np.full((1024, 2048, 3), 40, np.uint8)
        x = 100 + 5 * f
        cv2.rectangle(img, (x, 200), (x + 40, 225), (230, 230, 230), -1)       # pig in the thermal half
        cv2.rectangle(img, (600, 600), (660, 640), (200, 200, 200), -1)        # deer (not a pig)
        video.write(img)
        rows.append(f"{f},1,{x},200,40,25,1,61,1.0,Sus scrofa (Wild boar),0,0,{0 if f % 10 == 0 else 1}")
        rows.append(f"{f},2,600,600,60,40,1,21,1.0,Cervus elaphus (Red deer),0,2,1")
    video.release()
    (root / f"{flight}_gt.txt").write_text("\n".join(rows) + "\n")


def test_converts_wild_boar_only_and_splits_by_flight(tmp_path):
    src, out = tmp_path / "bambi", tmp_path / "yolo"
    for flight in ("101", "102", "103"):
        make_flight(src, flight)
    cmd = [sys.executable, str(TOOLS / "bambi_to_yolo.py"), "--bambi-root", str(src), "--out", str(out), "--stride", "10", "--negatives", "0", "--check", "1"]
    subprocess.run(cmd, check=True, capture_output=True, text=True)

    assert (out / "data.yaml").read_text().count("0: pig") == 1
    flights_per_split = {}
    for split in ("train", "val", "test"):
        labels = sorted((out / "labels" / split).glob("*.txt"))
        flights_per_split[split] = {p.name.split("_")[0] for p in labels}
        for p in labels:
            lines = p.read_text().split()
            assert len(lines) == 5, "one pig per frame; the deer must not be labelled"
            cls, cx, cy, w, h = map(float, lines)
            assert cls == 0 and abs(w - 40 / 1024) < 1e-4 and abs(h - 25 / 1024) < 1e-4
    # every flight in exactly one split (no leakage)
    all_flights = [f for s in flights_per_split.values() for f in s]
    assert sorted(all_flights) == ["101", "102", "103"]
    assert len(list((out / "check").glob("*.jpg"))) == 1

    # first kept frame of a flight: x = 100 -> centre (100 + 20) / 1024
    first = sorted((out / "labels").rglob("*_000000.txt"))[0]
    cx = float(first.read_text().split()[1])
    assert abs(cx - 120 / 1024) < 1e-4


def test_official_bambi_split_is_used_when_given(tmp_path):
    src, out = tmp_path / "bambi", tmp_path / "yolo"
    for flight in ("101", "102", "103"):
        make_flight(src, flight, frames=12)
    splits = tmp_path / "splits.json"
    splits.write_text('{"train": [101, 102], "val": [], "test": [103]}')
    cmd = [sys.executable, str(TOOLS / "bambi_to_yolo.py"), "--bambi-root", str(src), "--out", str(out),
           "--stride", "10", "--negatives", "0", "--bambi-splits", str(splits)]
    subprocess.run(cmd, check=True, capture_output=True, text=True)
    flights = lambda split: {p.name.split("_")[0] for p in (out / "labels" / split).glob("*.txt")}
    assert flights("train") == {"101", "102"} and flights("test") == {"103"} and flights("val") == set()


def test_stats_mode_needs_no_video_and_ranks_flights(tmp_path):
    ann = tmp_path / "annotations"
    ann.mkdir()
    (ann / "7_gt.txt").write_text("1,1,10,10,20,12,1,61,1.0,Sus scrofa (Wild boar),0,2,0\n2,1,11,10,20,12,1,61,1.0,Sus scrofa (Wild boar),0,2,1\n")
    (ann / "9_gt.txt").write_text("".join(f"{f},1,10,10,20,12,1,61,1.0,Sus scrofa (Wild boar),0,2,0\n" for f in range(5))
                                  + "1,2,50,50,30,20,1,21,1.0,Cervus elaphus (Red deer),0,2,0\n")
    (ann / "8_gt.txt").write_text("1,1,10,10,20,12,1,21,1.0,Cervus elaphus (Red deer),0,2,0\n")  # no pigs
    splits = tmp_path / "splits.json"
    splits.write_text('{"train": [9], "val": [], "test": [7]}')
    out = tmp_path / "stats.csv"
    subprocess.run([sys.executable, str(TOOLS / "bambi_to_yolo.py"), "--bambi-root", str(ann), "--stats", str(out),
                    "--bambi-splits", str(splits), "--stride", "1"], check=True, capture_output=True, text=True)
    import csv as _csv
    rows = list(_csv.DictReader(out.open()))
    assert [r["flight"] for r in rows] == ["9", "7"]          # ranked by keyframes; flight 8 has no pigs
    assert rows[0]["split"] == "train" and rows[0]["pig_keyframes"] == "5" and "red deer" in rows[0]["other_species"]
    assert rows[1]["pig_keyframes"] == "1" and rows[1]["pig_frames"] == "2" and rows[1]["has_video"] == "False"
