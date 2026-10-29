# 16 · ML hand-over: train the pig detectors, then plug them into the app

> 📁 ML docs live in `src/ml/docs/` (moved from repo `docs/16-ml-handoff.md` on 28 Oct 2026). Index: [README.md](README.md).


> **Owner:** teammate (ML). **App integration (Part E):** Gaurav, or the teammate pairing with him.
> **Read first:** this file. Details live in 03-bambi-training.md (BAMBI script and Colab), 06-nacas-label-train-test.md (NACAS labelling) and
> 04-ml-plan.md (ML plan), plus ../README.md (all commands).
> **Report back:** `✅ <part>.<step>` or `❌ <part>.<step>` plus the first error line only.

## 0. The big picture
```
             ┌────────── trained on BAMBI (public) in Colab ──────────┐   fine-tuned on NACAS (CDU) on Gaurav's PC
             ▼                                                        ▼
  yolov8s-bambi-thermal.pt   yolov8s-bambi-rgb.pt        ──►   yolov8s-nacas-thermal.pt  (better on NT pigs)
             │                       │
             └────── src/ml detection service (FastAPI, port 8000): /detect?camera=thermal|rgb ──────┐
                                                                                                      ▼
  .NET API "Run AI" ──► each photo ──► service ──► boxes saved as AI / Pending ──► Review tool (amber boxes)
                                                                       ranger accepts/rejects ──► count ──► estimate
```
The AI only **suggests**; a ranger confirms every box, and the research model corrects the count. A weaker model
slows the review down, but it never makes the estimate wrong.

## 1. Rules
1. **NACAS photos never leave Gaurav's PC.** That rules out Colab, Google Drive, GitHub, chat and AI tools. Their
   fine-tuning (Part C) is done **on Gaurav's PC**. You won't receive the photos.
2. **BAMBI only on Colab.** Never upload the raw videos; upload the zips only.
3. **Never commit** `*.pt`, images, videos or datasets. Share results through the Drive `results/` folder.
4. **Report real numbers**, including the no-training baseline.

## 2. What is in `src/ml` (checked 28 Oct)
| Part | File | Status |
|---|---|---|
| Detection service | `aerialwatch_ml/service.py`, `detector.py`, `config.py`, `schemas.py` | ✅ **One model per camera** (`/detect?camera=thermal\|rgb`), **tiling** for large RGB photos, `/models` endpoint. Thermal falls back to the hotspot baseline; RGB without a model answers 503. |
| BAMBI → YOLO | `tools/bambi_to_yolo.py` | ✅ Thermal, plus **RGB using BAMBI's own RGB boxes** (`<id>_rgb_gt.txt`; before this fix it wrongly reused the thermal boxes) |
| Training | `tools/train.py` | ✅ `--camera`, `--freeze`, `--lr0`, `--device`, `--data-source`; version name `yolov8s-<name>-<date>` |
| Evaluation | `tools/evaluate.py` | ✅ jpg/png, `--tile` for large photos, baseline comparison |
| Colab | `notebooks/train_yolo_colab.ipynb` | ✅ One `CAMERA = "thermal"` / `"rgb"` switch; saves to `results/<name>/` |
| PC helper | `tools/bambi_pipeline.ps1` | ✅ Steps 1–9 thermal, **10–12 RGB** |
| NACAS export | App: Admin › AI training data (D-038); `tools/export_reviews_to_yolo.py` is the fallback | ✅ |
| Tests | `tests/` (converter, baseline, metrics, API, **tiling**) | Run in step A.1 |

## Part A: BAMBI thermal model (start today)
1. **Run the helper script.** On your PC, from the repo root:
   - Start it with `powershell -ExecutionPolicy Bypass -File src\ml\tools\bambi_pipeline.ps1`.
   - Run **steps 1–9** in order. Full explanations are in 03-bambi-training.md.
   - Step 6 runs the ML tests. **All must pass**; send the first failure otherwise.
2. **Check the boxes.** In step 8, the green boxes must sit on the pigs.
3. **Train in Colab.**
   - Upload `ml.zip` and `yolo-bambi.zip` to `MyDrive/AerialWatch/`.
   - Open the notebook, keep `CAMERA = "thermal"` and run all cells (about 1–2 h on a T4).
4. **Send it back.** Share `MyDrive/AerialWatch/results/bambi-thermal/` with Gaurav. It should contain:
   - `yolov8s-bambi-thermal-<date>.pt` and its `.json` card
   - the metrics `.md` and `.json` files for **YOLO and the baseline**
   - `results.png` and `PR_curve.png`

**Done when** the YOLO test metrics exist next to the baseline. Targets: recall ≥ 0.8, precision ≥ 0.6. Report the real numbers either way.

## Part B: BAMBI RGB model (same videos, no new download)
1. **Step 10, download the RGB boxes.** This fetches BAMBI's RGB boxes (`--version owl-transferred`, annotations only, a few MB). It reports how many of the 22 flights have RGB boxes; flights without them are skipped, which is expected.
2. **Step 11, convert.** This builds `C:\aw\yolo-bambi-rgb` from the right half of each video.
3. **Step 12, check images.** The green boxes must sit on the animal's spot, even if the pig is hard to see.
   - Boxes shifted sideways → run step 11 again with x-offset `1024`.
   - Boxes one frame behind → frame-offset `1`.
4. **Step 9 again.** This also zips `yolo-bambi-rgb.zip`. Upload it to `MyDrive/AerialWatch/`.
5. **Colab.** Set `CAMERA = "rgb"` and run all cells. Share `results/bambi-rgb/`.

**Expect lower numbers than thermal.** Pigs are much harder to see in colour, and the RGB boxes were transferred
from thermal. The app labels this model **experimental**.

## Part C: NACAS fine-tune (on Gaurav's PC, after Part A)
Use the BAMBI thermal model as the starting point, so it learns NT conditions from about 120 labelled NACAS photos.
1. **Label (Gaurav, in the app; 06-nacas-label-train-test.md):**
   - Create 12 thermal surveys, one per flight folder, using the `_THM.JPG` files.
   - Draw a box on every pig, then mark the photo checked.
   - Set the training set on each survey page: 14 Feb → Test, 13 Feb 10:00 → Validation, the rest → Train.
2. **Export.** Admin › AI training data → Thermal → **Export dataset**. Note the folder:
   `C:\AerialWatchData\training-exports\wfp\thermal-<date>-<time>`.
3. **Get the size.** The thermal photo width, rounded up to a multiple of 32, is `<W>` below (for example 640). Get it from the resolution check in 05-nacas-data-rules.md.
4. **Run the commands.** In `src\ml`, with the `.venv` active and `pip install -r requirements-train.txt` done:
   ```powershell
   $D = "C:\AerialWatchData\training-exports\wfp\thermal-<date>-<time>"
   $B = "models\yolov8s-bambi-thermal-<date>.pt"          # from Part A
   python tools/evaluate.py --data $D --split test --baseline --out C:\NACAS-data\results\baseline
   python tools/evaluate.py --data $D --split test --weights $B --imgsz <W> --out C:\NACAS-data\results\before
   python tools/train.py --data $D\data.yaml --model $B --epochs 40 --batch 4 --imgsz <W> --freeze 10 --lr0 0.002 `
       --name nacas-thermal --data-source "NACAS (CDU), local only" --device cpu       # --device 0 with an NVIDIA GPU
   python tools/evaluate.py --data $D --split test --weights models\yolov8s-nacas-thermal-<date>.pt --imgsz <W> --out C:\NACAS-data\results\after
   ```
   Training on a CPU takes roughly 1–3 h, so leave it running overnight. The model stays in `models\` (git-ignored, local).
5. **Record the evidence.** Put **numbers only** in `evidence/<date>-ml/nacas-results.md`, never images:

| Model | Precision | Recall | F1 | AP50 | Count error |
|---|---|---|---|---|---|
| Hotspot baseline | | | | | |
| BAMBI only (Austria) | | | | | |
| BAMBI + NACAS fine-tune | | | | | |

The RGB fine-tune on NACAS is **not before submission**. It needs RGB labels made with the thermal pair as a guide.

## Part D: Run the detection service with the trained models
On the PC that runs the app (Gaurav's):
1. Copy the `.pt` and `.json` files into `src\ml\models\`.
2. Start the service:
   ```powershell
   cd src\ml; .venv\Scripts\activate; pip install -r requirements-train.txt
   $env:AW_MODEL_PATH_THERMAL="models\yolov8s-nacas-thermal-<date>.pt"   # or the BAMBI thermal model
   $env:AW_MODEL_VERSION_THERMAL="yolov8s-nacas-thermal-<date>"
   $env:AW_IMAGE_SIZE_THERMAL="<W>"                                       # same as training (1024 for BAMBI)
   $env:AW_MODEL_PATH_RGB="models\yolov8s-bambi-rgb-<date>.pt"; $env:AW_MODEL_VERSION_RGB="yolov8s-bambi-rgb-<date>"
   $env:AW_MIN_CONFIDENCE="<best-F1 threshold from evaluate>"
   uvicorn aerialwatch_ml.service:app --port 8000
   ```
3. Check it at `http://localhost:8000/docs`:
   - `GET /models` should show both cameras as `available: true`.
   - `POST /detect` with a photo should return boxes.

## Part E: Connect to .NET and show it in the UI (sprint task 3, after Part A)
Same layering as the rest of the app. The browser never calls the ML service; the .NET API does.

### E.1 .NET backend
| Layer | File | What |
|---|---|---|
| Config | `appsettings.json` | `"Detection": { "BaseUrl": "http://localhost:8000", "TimeoutSeconds": 60, "BatchSize": 10 }` |
| Application (port) | `Application/Wfp/Detection/IDetectionClient.cs` | `DetectAsync(Stream photo, string fileName, CameraType camera, ct)` returns `DetectionResult(ModelVersion, Width, Height, Boxes)`; `GetModelsAsync(ct)` |
| Application | `Wfp/Detection/AiDetectionService.cs` (implements Contracts `IAiDetectionService`) | `RunBatchAsync(surveyId)`: see the rules below. `GetStatusAsync(surveyId)` returns photos, processed, remaining, AI boxes, and model available yes/no |
| Infrastructure | `Wfp/Detection/HttpDetectionClient.cs` | Typed `HttpClient`: multipart `POST /detect?camera=<thermal\|rgb>`; maps JSON to `DetectionResult`; service down → a friendly "AI service is not running" error |
| API | `Controllers/Wfp/DetectionController.cs` | `GET /api/wfp/detection/models` · `GET /api/wfp/detection/status/{surveyId}` · `POST /api/wfp/detection/run/{surveyId}` |
| Tests | `UnitTests/Wfp/Detection/` | Fake `IDetectionClient`: boxes saved as Ai/Pending with the model version; photos already processed are skipped; wrong status is refused; no model → clear error |

**Rules for `RunBatchAsync`:**
- **When:** only while photos can still change (survey `Uploading`), or before review starts. The status becomes
  `Processing` while it runs. `ReviewRules.CanStart` already accepts `Processing`.
- **Batching:** each call handles the next `BatchSize` photos that have **no AI boxes yet** and returns
  `{ processed, remaining, boxesAdded }`. The frontend calls it again until `remaining = 0`. This avoids an HTTP timeout
  on 200 photos and needs no background jobs.
- **Each photo:**
  1. Open the original from `IFileStorage` and call the client with the **survey's camera**.
  2. Save each box as `AnimalDetection { Source = Ai, Status = Pending, Confidence, ModelVersion, Label = "pig" }`.
     Boxes are already in original pixels.
- **No model for this camera** (RGB without weights → 503): stop with the message "No RGB model yet; review these
  photos manually." The survey is unchanged.
- **Never delete** reviewed AI boxes (D-034).

### E.2 Frontend
| Where | What |
|---|---|
| Survey page, next to *Review photos* | **Run AI** button showing which model runs, e.g. "Thermal · yolov8s-nacas-thermal" or "RGB · experimental". A progress bar (processed / total) loops `run` until `remaining = 0`, then reads "N suggestions ready → Review photos". Disabled with a tooltip when there's no model for this camera |
| Review tool | Nothing new needed: AI boxes already show as **amber dashed "AI 87%"**, with accept/reject, bulk-accept above a confidence slider, and progress counts (D-034). Add a small chip in the header with the model version |
| Admin › AI model (optional) | Cards per camera: model version, kind (YOLO or baseline), available, threshold; plus the test metrics table copied from the model card and metrics `.json` |
| Results page (Slice 5) | Uses only **confirmed** boxes. Optionally shows "AI found X of Y confirmed pigs" (AI accepted vs ranger-drawn) as a live accuracy figure |

### E.3 Start everything for the demo
1. Start the ML service (Part D).
2. Start the .NET API.
3. Start the frontend.
4. Create a survey, upload photos, then **Run AI**, **Review**, **Complete review**, and open **Results**.

If the ML service isn't running, the app still works: rangers review manually and Run AI explains why it's unavailable.

## 3. Timeline (submission Thu 30 Oct)
| When | Teammate | Gaurav |
|---|---|---|
| Tue 28 | A.1–A.3 (download overnight) | Label NACAS thermal (C.1), Slice 5 Results |
| Wed 29 | A.4, then B (RGB) | E.1–E.2 Run AI (with the hotspot baseline until the weights arrive) |
| Wed 29 evening | — | C.2–C.4 NACAS fine-tune overnight on his PC |
| Thu 30 | Metrics table and evidence | Part D with the final weights, demo video, submit |

## 4. Licences and credit (for the README)
- **Ultralytics YOLOv8:** AGPL-3.0 (the tool is open source; NACAS gets the source).
- **BAMBI:** CC BY 4.0, cited.
- **NACAS images:** used with permission, local only, credited as NACAS wishes (05-nacas-data-rules.md).
