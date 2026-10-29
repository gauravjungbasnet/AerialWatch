# 08 · Detection service API and integration with .NET and the frontend

> 📁 Reference for whoever builds "Run AI" (sprint task 3, [01-ml-handoff](01-ml-handoff.md) Part E).
> Code: `aerialwatch_ml/service.py`, `detector.py`, `config.py`, `schemas.py`.

## 1. Who calls whom
```
Browser ──► .NET API (/api/wfp/detection/...) ──► ML service (http://localhost:8000, internal only) ──► YOLO / baseline
                   │                                        ▲
                   └── reads photo bytes from IFileStorage ─┘   boxes come back in ORIGINAL pixels
```
The browser **never** calls the ML service (AGENTS.md). The API owns authentication, storage and the database.

## 2. ML service endpoints (FastAPI, docs at `/docs`)
| Method | Path | Answer |
|---|---|---|
| GET | `/health` | `{"status":"ok"}` |
| GET | `/models` | List of both cameras: `[{camera, available, model_version, kind, classes, min_confidence, image_size, tiled, notes}]` |
| GET | `/model?camera=thermal\|rgb` | One of the above |
| POST | `/detect?camera=thermal\|rgb[&min_confidence=0.3]` | Multipart `file` (JPEG/PNG), answers `DetectResponse` |

`DetectResponse`:
```json
{ "camera": "thermal", "model_version": "yolov8s-nacas-thermal-2026-10-29",
  "image_width": 640, "image_height": 512, "elapsed_ms": 84.2,
  "boxes": [ { "x": 312.0, "y": 140.5, "width": 22.0, "height": 15.0, "label": "pig", "confidence": 0.87 } ] }
```
- Boxes use **original-image pixels, top-left origin**, the same as `wfp.Detections`. The EXIF rotation is applied first, so they match what the browser shows.
- Errors:
  - `400`: not an image, or empty
  - `413`: larger than `AW_MAX_UPLOAD_MB`
  - `422`: bad camera or confidence
  - `503`: no detector for that camera (RGB without weights)

## 3. Configuration (environment variables)
| Variable | Default | Meaning |
|---|---|---|
| `AW_MODEL_PATH_THERMAL` (or `AW_MODEL_PATH`) | none, which means the hotspot baseline | Thermal YOLO weights |
| `AW_MODEL_VERSION_THERMAL` (or `AW_MODEL_VERSION`) | file name | Stored on every box (`ModelVersion`) |
| `AW_IMAGE_SIZE_THERMAL` (or `AW_IMAGE_SIZE`) | 1024 | Must equal `train.py --imgsz` |
| `AW_MODEL_PATH_RGB`, `AW_MODEL_VERSION_RGB`, `AW_IMAGE_SIZE_RGB` | none, which means 503 | RGB model |
| `AW_TILE_THERMAL` / `AW_TILE_RGB` | false / **true** | Tile large photos before detection |
| `AW_TILE_SIZE`, `AW_TILE_OVERLAP` | 1280, 0.2 | Tile size and overlap |
| `AW_MIN_CONFIDENCE` | 0.25 | Set it to the best-F1 threshold from `evaluate.py` |
| `AW_MAX_UPLOAD_MB` | 50 | Upload limit |

## 4. .NET side (to build; same layering as the rest of the app)
| Layer | Type | Notes |
|---|---|---|
| Contracts `Wfp/Detection/Services` | `IAiDetectionService` | `GetModelsAsync`, `GetStatusAsync(surveyId)`, `RunBatchAsync(surveyId)` |
| Contracts `Wfp/Detection/Models` | `DetectionModelDto`, `AiRunStatusDto(Photos, Processed, Remaining, BoxesAdded, ModelVersion, Available)` | |
| Application `Wfp/Detection` | `IDetectionClient` (port), `AiDetectionService` | See the rules below |
| Infrastructure `Wfp/Detection` | `HttpDetectionClient` (typed `HttpClient`, `Detection:BaseUrl`, timeout) | Multipart POST; maps snake_case JSON; turns connection errors into a friendly message |
| Api `Controllers/Wfp/DetectionController` | `GET models`, `GET status/{surveyId}`, `POST run/{surveyId}` | `/api/wfp/detection/...` |
| Config | `"Detection": { "BaseUrl": "http://localhost:8000", "TimeoutSeconds": 60, "BatchSize": 10 }` | |

**`RunBatchAsync` rules:**
1. Allowed before review (survey `Uploading` or `Processing`). Set the status to `Processing`.
2. Take the next `BatchSize` photos that have **no AI boxes yet**.
3. For each photo:
   - Read it from `IFileStorage` and call `/detect?camera=<survey camera>`.
   - Save each box as `AnimalDetection { Source = Ai, Status = Pending, Confidence, ModelVersion, Label = "pig" }`.
4. Return `{processed, remaining, boxesAdded}`. The frontend repeats until `remaining = 0`.
5. 503 from the service: stop with "No RGB model yet; review these photos manually". Nothing is changed.
6. Never delete reviewed AI boxes (D-034). They are the live accuracy data.

**Unit tests:** use a fake `IDetectionClient` to check that boxes are saved as Ai/Pending, processed photos are skipped, a wrong status is refused, and a missing model gives a clear error.

## 5. Frontend
| Where | What |
|---|---|
| Survey page | **Run AI** button showing the model, e.g. "Thermal · yolov8s-nacas-thermal" or "RGB · experimental". A progress bar (processed / total) repeats `run` until done, then shows "N suggestions → Review photos". Disabled with a tooltip if there's no model for the camera |
| Review tool | Already done (D-034): amber dashed "AI 87%" boxes, accept/reject, bulk-accept slider. Add a model-version chip |
| Admin › AI model (optional) | Per camera: version, kind, available, threshold, plus the test metrics from the model card JSON |
| Results (Slice 5) | Confirmed boxes only. Optional "AI found X of Y confirmed pigs" |

## 6. Run everything for the demo
1. Start the ML service: `uvicorn aerialwatch_ml.service:app --port 8000` (with the env vars above).
2. Start the .NET API.
3. Start the frontend.
4. Survey → upload → **Run AI** → **Review** → **Complete review** → **Results**.

If the ML service is down, the app still works with manual review.
