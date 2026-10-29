# AGENTS.md · src/ml (rules for AI coding agents and new contributors)

> Applies to everything under `src/ml/`. The repo-wide rules in the root `AGENTS.md` still apply.
> Docs index: [docs/README.md](docs/README.md). Commands: [README.md](README.md).

## 1. Data rules (non-negotiable)
1. **NACAS (CDU) imagery is restricted** ([docs/05](docs/05-nacas-data-rules.md)).
   - Never read, upload, paste, stage or describe NACAS image *contents* in any cloud tool, AI assistant, Colab, Google Drive, GitHub, Kaggle or Hugging Face.
   - Agents may work with **file names, sizes and aggregate numbers** only.
   - NACAS training and evaluation run **on Gaurav's PC**.
2. **Never commit** weights (`models/*.pt`, `*.onnx`), datasets, images, videos, `runs/`, or anything under `C:\NACAS-data` or `C:\AerialWatchData`.
3. **BAMBI** (CC BY 4.0) may be used on Colab. Cite it. Don't use its `environment-nc` layers.
4. **Evidence** for NACAS = numbers only (metrics tables, counts). Screenshots use BAMBI or synthetic frames.

## 2. Layout
```
aerialwatch_ml/   service (FastAPI): service.py · detector.py (YOLO, hotspot baseline, tiling) · config.py (env) · schemas.py (wire format)
tools/            bambi_to_yolo.py · train.py · evaluate.py · export_reviews_to_yolo.py (fallback) · bambi_pipeline.ps1 (Windows helper)
notebooks/        train_yolo_colab.ipynb (BAMBI only; CAMERA switch)
tests/            pytest: converter, baseline, metrics, API, tiling
models/           weights + model cards (.json); weights git-ignored
docs/             all ML documentation (01–10)
```

## 3. Contracts that must not break
- **Boxes = original-image pixels, top-left origin, after EXIF rotation**, everywhere: service, API, database (`wfp.Detections`), viewer. Never return letterboxed or resized coordinates.
- `POST /detect?camera=thermal|rgb` returns `DetectResponse` (`camera, model_version, image_width, image_height, boxes[], elapsed_ms`). If you change it, update `schemas.py`, `tests/test_service.py`, [docs/08](docs/08-service-api-and-integration.md) and the .NET `HttpDetectionClient` together.
- Thermal without weights → `HotspotDetector`. RGB without weights → **503** (no fake RGB detector).
- Class name `pig` (single class, id 0). `PIG_LABELS` maps other names to it.
- The browser never calls this service; only the .NET API does.

## 4. Training rules
- **Split by flight/survey, never by frame or photo** (leakage). BAMBI uses the official `splits.json`; NACAS keeps 14 Feb as test.
- `--imgsz` at training = `AW_IMAGE_SIZE_<CAMERA>` at inference.
- Augmentation: rotations and flips yes; **no colour jitter** (thermal palettes).
- RGB labels come from `<id>_rgb_gt.txt`, never from the thermal boxes.
- Always evaluate on the **test** split and report it **next to the hotspot baseline** (and BAMBI-only next to the fine-tuned model). Report real numbers.
- Model name `yolov8s-<dataset>-<date>`; keep the `.json` card with every `.pt`.

## 5. Commands (Windows, from `src\ml`)
```powershell
py -3.12 -m venv .venv; .venv\Scripts\activate
pip install -r requirements-dev.txt            # service + tests (no ultralytics)
python -m pytest -q tests                      # must pass before any commit
pip install -r requirements-train.txt          # + ultralytics, for training/YOLO inference
uvicorn aerialwatch_ml.service:app --port 8000 # http://localhost:8000/docs
```
Python 3.11 or 3.12 (3.14 lacks wheels).

## 6. Working style in this repo
- Small, file-by-file changes. The human runs the builds and tests and reports `✅ step N` or the first error only.
- Don't use reserved words as identifiers (e.g. `unchecked` in C#, `lambda`/`type` shadowing in Python).
- Update the matching doc in `docs/` and add a row to [docs/10](docs/10-decisions-and-changelog.md) when behaviour changes.
