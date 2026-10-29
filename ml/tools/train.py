"""Train (or fine-tune) the pig detector with Ultralytics YOLOv8 and save a versioned model + model card.

BAMBI on a GPU (Google Colab T4; see notebooks/train_yolo_colab.ipynb):

    python tools/train.py --data <yolo-bambi>/data.yaml --model yolov8s.pt --epochs 60 --imgsz 1024 --name bambi-thermal
    python tools/train.py --data <yolo-bambi-rgb>/data.yaml --model yolov8s.pt --epochs 60 --imgsz 1024 --name bambi-rgb --camera rgb

NACAS fine-tune, LOCAL ONLY (docs/11): start from the BAMBI weights, small learning rate, frozen backbone:

    python tools/train.py --data <export>/data.yaml --model models/yolov8s-bambi-thermal-<date>.pt --epochs 40 --batch 4 ^
        --imgsz 640 --freeze 10 --lr0 0.002 --name nacas-thermal --data-source "NACAS (CDU), local only" --device cpu

Output: models/<family>-<name>-<date>.pt and the matching .json model card (data, settings, test metrics).
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import shutil
from pathlib import Path


def main() -> None:
    ap = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--data", type=Path, required=True)
    ap.add_argument("--model", default="yolov8s.pt", help="Starting weights: yolov8n.pt (fast), yolov8s.pt (default), or a previous run")
    ap.add_argument("--epochs", type=int, default=60)
    ap.add_argument("--imgsz", type=int, default=1024, help="Pigs are small; 1024 keeps them visible (must match AW_IMAGE_SIZE_<CAMERA>)")
    ap.add_argument("--batch", type=int, default=8)
    ap.add_argument("--name", default="bambi", help="Short dataset name in the version, e.g. bambi, bambi-rgb, nacas")
    ap.add_argument("--camera", choices=["thermal", "rgb"], default="thermal")
    ap.add_argument("--data-source", default="BAMBI (CC BY 4.0)", help="Recorded in the model card")
    ap.add_argument("--freeze", type=int, default=0, help="Freeze the first N layers (10 = backbone) when fine-tuning on few photos")
    ap.add_argument("--lr0", type=float, default=None, help="Initial learning rate (default: Ultralytics'); lower it for fine-tuning")
    ap.add_argument("--device", default=None, help="e.g. 0 (first GPU) or cpu; default: automatic")
    ap.add_argument("--models-dir", type=Path, default=Path(__file__).resolve().parents[1] / "models")
    args = ap.parse_args()

    from ultralytics import YOLO

    extra = {}
    if args.lr0 is not None:
        extra["lr0"] = args.lr0
    if args.device is not None:
        extra["device"] = args.device

    model = YOLO(args.model)
    model.train(
        data=str(args.data), epochs=args.epochs, imgsz=args.imgsz, batch=args.batch, name=args.name,
        patience=15,          # stop early when val mAP stops improving
        fliplr=0.5, flipud=0.5, degrees=180,   # nadir drone view: no "up", any rotation is realistic
        hsv_h=0.0, hsv_s=0.0,                  # thermal palettes: don't invent colours
        mosaic=1.0, close_mosaic=10,
        freeze=args.freeze or None,
        seed=2026, deterministic=True,
        **extra,
    )
    best = Path(model.trainer.best)
    metrics = YOLO(str(best)).val(data=str(args.data), imgsz=args.imgsz, split="test", **({"device": args.device} if args.device else {}))

    family = Path(args.model).stem.split("-")[0]   # yolov8s.pt -> yolov8s; yolov8s-bambi-2026-10-28.pt -> yolov8s
    version = f"{family}-{args.name}-{dt.date.today():%Y-%m-%d}"
    args.models_dir.mkdir(parents=True, exist_ok=True)
    target = args.models_dir / f"{version}.pt"
    shutil.copy(best, target)
    card = {
        "model_version": version,
        "camera": args.camera,
        "base_weights": args.model,
        "data": str(args.data),
        "epochs": args.epochs,
        "imgsz": args.imgsz,
        "freeze": args.freeze,
        "lr0": args.lr0,
        "test_map50": round(float(metrics.box.map50), 4),
        "test_map50_95": round(float(metrics.box.map), 4),
        "test_precision": round(float(metrics.box.mp), 4),
        "test_recall": round(float(metrics.box.mr), 4),
        "licences": {"weights_code": "Ultralytics YOLOv8, AGPL-3.0", "training_data": args.data_source},
    }
    (args.models_dir / f"{version}.json").write_text(json.dumps(card, indent=2))
    print(json.dumps(card, indent=2))
    up = args.camera.upper()
    print(f"\nService: set AW_MODEL_PATH_{up}={target} and AW_MODEL_VERSION_{up}={version} (AW_IMAGE_SIZE_{up}={args.imgsz}).")


if __name__ == "__main__":
    main()
