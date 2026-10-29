# ml/: pig detection service and training tools (D-035)

> **Docs:** [docs/README.md](docs/README.md) (hand-over, guides, knowledge base). **Rules for agents:** [AGENTS.md](AGENTS.md).


The AI suggests boxes. A ranger accepts, rejects or adds to them in the review tool (D-034), and only confirmed boxes
are counted. The research model then corrects the count (D-006, D-007). So the AI makes the ranger faster, but it never
decides the number on its own.

```
ml/
  aerialwatch_ml/          the service (FastAPI): /detect, /model, /health
    detector.py            YoloDetector (trained) | HotspotDetector (no-training baseline), behind one interface
  tools/
    bambi_to_yolo.py       BAMBI flights (video + MOT annotations) -> YOLO dataset, wild boar only, split by flight
    train.py               YOLOv8 training with our defaults -> models/<version>.pt + model card
    evaluate.py            precision / recall / AP50 / count error on held-out flights -> evidence markdown
  notebooks/train_yolo_colab.ipynb   the same steps on a free Colab GPU
  tests/                   pytest: converter, baseline, metrics, API, tiling
  models/                  weights (git-ignored) + model cards (.json)
```

## The plan, step by step

| # | Step | Where | Output |
|---|---|---|---|
| 1 | Download wild-boar flights from BAMBI (annotations first, then ~15–25 videos) | your PC | `data/bambi/<flight>_gt.txt`, `<flight>_*.mp4` |
| 2 | Convert to YOLO, check the preview images | your PC or Colab | `data/yolo-bambi/` + `dataset-card.json` |
| 3 | Train YOLOv8s at 1024 px, about 60 epochs | Colab T4 | `models/yolov8s-bambi-<date>.pt` + card |
| 4 | Evaluate on test flights, trained model **and** baseline | Colab | `metrics-*.md` → `evidence/<date>-ml/` |
| 5 | Run the service with the weights; .NET calls it after upload | your PC | boxes appear in the review tool |
| 6 | NACAS photos: label ~100–200 in the review tool, export, fine-tune, re-evaluate | later | `…-nacas` model + NT metrics |

### 1. Download BAMBI (wild boar only)
```powershell
git clone https://github.com/bambi-eco/Dataset.git bambi-tools
cd bambi-tools; pip install -r requirements.txt
python filter_flights.py --folder flight_metadata --species "Sus scrofa" -v      # list flight ids (names are "Sus scrofa (Wild boar)")
python download_from_zenodo.py --annotations-only -f <id>                         # a few MB: check box counts first
python download_from_zenodo.py -f <id> --unzip                                    # then the video
```
**Choose before downloading videos:** download annotations only for every boar flight
(`python download_from_zenodo.py --annotations-only -f <ids> -o ../annotations`), then rank them:
```powershell
python tools/bambi_to_yolo.py --bambi-root ../../data/bambi/annotations --stats ../../evidence/<date>-ml/flight-stats.csv `
    --bambi-splits ../../data/bambi/Dataset/flight_metadata/splits.json --metadata ../../data/bambi/Dataset/flight_metadata
```
Pick ~20 (≈14 train, 2 val, 4–5 test by the official split), then download only those videos.
Put the files in `CodingCompetition/data/bambi/` (git-ignored). The full dataset is about 350 GB. **Only take wild-boar
flights**, preferring ones with many annotated frames, and spread across different dates and sites.

### 2. Convert
```powershell
cd src/ml
py -3.12 -m venv .venv; .venv\Scripts\activate; pip install -r requirements-dev.txt   # Python 3.11/3.12 (3.14 lacks wheels)
python tools/bambi_to_yolo.py --bambi-root ../../data/bambi --out ../../data/yolo-bambi --stride 1 --check 20 `
    --bambi-splits ../../data/bambi/Dataset/flight_metadata/splits.json
```
Open `data/yolo-bambi/check/*.jpg`. **The green boxes must sit on the pigs.** If they're shifted sideways, the
annotations use full-frame coordinates (`--x-offset 1024`). If they're one frame behind, use `--frame-offset 1`.

### 3–4. Train and evaluate (Colab)
Upload `ml.zip` and `yolo-bambi.zip` to `MyDrive/AerialWatch/`, open `notebooks/train_yolo_colab.ipynb` in Colab,
select a T4 GPU and run all cells. Copy the weights to `src/ml/models/` and the metrics to `evidence/<date>-ml/`.

### 5. Run the service
```powershell
pip install -r requirements-train.txt   # includes ultralytics (needed to load YOLO weights)
# without weights: thermal uses the hotspot baseline, RGB answers 503 (no detector yet)
uvicorn aerialwatch_ml.service:app --port 8000
# with trained models (one per camera)
$env:AW_MODEL_PATH_THERMAL="models/yolov8s-bambi-thermal-2026-10-28.pt"; $env:AW_MODEL_VERSION_THERMAL="yolov8s-bambi-thermal-2026-10-28"
$env:AW_MODEL_PATH_RGB="models/yolov8s-bambi-rgb-2026-10-28.pt";         $env:AW_MODEL_VERSION_RGB="yolov8s-bambi-rgb-2026-10-28"
uvicorn aerialwatch_ml.service:app --port 8000
```
`curl -F "file=@DJI_0001_THM.JPG" "http://localhost:8000/detect?camera=thermal"` returns
`{ camera, model_version, image_width, image_height, boxes: [{x, y, width, height, label, confidence}], elapsed_ms }`.
Boxes use original-image pixels with a top-left origin, the same as `wfp.Detections`, so .NET inserts them as they
are (`Source = Ai`, `Status = Pending`, `ModelVersion`). `GET /models` lists both cameras (available or not).

Settings (`aerialwatch_ml/config.py`): `AW_MODEL_PATH_<CAMERA>`, `AW_MODEL_VERSION_<CAMERA>`, `AW_IMAGE_SIZE_<CAMERA>`
(1024, same as training), `AW_TILE_<CAMERA>` (RGB tiled by default: large photos are cut into 1280 px tiles with 20 %
overlap and the boxes merged), `AW_TILE_SIZE`, `AW_TILE_OVERLAP`, `AW_MIN_CONFIDENCE` (0.25; use the best-F1 value from
`evaluate.py`), `AW_MAX_UPLOAD_MB` (50). `AW_MODEL_PATH`/`AW_MODEL_VERSION` still work for thermal.

Full hand-over (BAMBI thermal + RGB, NACAS fine-tune, connecting to .NET and the UI): [docs/01-ml-handoff.md](docs/01-ml-handoff.md).

## Tests
```powershell
python -m pytest -q tests
```

## Honest limits (say these to the judges)
- BAMBI is **European forest wild boar**. NT feral pigs are the same species, but the vegetation, heat and camera
  differ. Metrics on BAMBI test flights are not NT field accuracy; only NACAS photos can give that (step 6).
- The detector suggests, a ranger confirms. The estimate never uses an unreviewed box.
- The hotspot baseline is a heuristic score, not a probability. It shows what the learned model adds, and keeps the
  app working without a GPU.

## Licences
Ultralytics YOLOv8 is **AGPL-3.0**. That's fine for this open-source tool (we give NACAS the source, D-009), but it
must be listed in `evidence/licences.md`. BAMBI data is CC BY 4.0, so cite it. Don't use its `environment-nc` layers.
FastAPI, OpenCV, Pillow and NumPy are MIT, Apache-2.0, HPND and BSD respectively.
