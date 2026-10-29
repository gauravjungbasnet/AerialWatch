# 14 · NACAS photos: label, train and test (local only)

> 📁 ML docs live in `src/ml/docs/` (moved from repo `docs/14-nacas-label-train-test.md` on 28 Oct 2026). Index: [README.md](README.md).


> Everything here runs on **Gaurav's PC only** (05-nacas-data-rules.md). NACAS photos, labels, the exported dataset and the fine-tuned
> weights never go to git, Colab, Drive or any AI tool.

## The idea
NACAS sent photos **without boxes**. We make the boxes ourselves in our own review tool. The app saves them in SQL, then
an export script turns them into a YOLO dataset. We fine-tune the BAMBI model on it and test on flights it never saw.

```
NACAS _THM photos ─► Aerial Watch (one survey per flight) ─► draw boxes in Review ─► wfp.Detections (SQL)
                                                                          │
           export_reviews_to_yolo.py ◄────────────────────────────────────┘
                     │
   C:\NACAS-data\yolo-nacas  (train / val / test, split by flight)
                     │
   evaluate BAMBI model as-is ─► fine-tune from BAMBI best.pt ─► evaluate again ─► compare (aggregate numbers only)
```

## What the app stores
- **The photo:** the original file, unchanged, on disk under `C:\AerialWatchData\wfp\surveys\<surveyId>\originals\`, plus a thumbnail.
- **The boxes:** rows in `wfp.Detections` (x, y, width, height in original pixels, `Status = Accepted`, who drew them and when). Nothing is drawn into the image file.
- **Progress:** `SurveyImages.Reviewed = 1` once a photo is marked checked (Space).
- `C:\AerialWatchData` is outside the repo. Check that it's not synced by OneDrive.

## Step 1: Label (about 2–3 h for about 200 thermal photos)
1. Move the data to `C:\NACAS-data\` (outside the repo).
2. In Aerial Watch, create **one survey per flight folder**:
   - Name: `NACAS <folder>`, e.g. `NACAS 140221_0600`.
   - Date and time from the folder name (`DDMMYY_HHMM`).
   - Camera: **Thermal**.
3. Upload only that folder's **`*_THM.JPG`** files. Leave out the RGB files for now.
4. Click **Start review**. On each photo:
   - Press `N` and drag a tight box around each pig.
   - Press `F` to flag a photo you're unsure about. Flagged photos are left out of training.
   - Press `Space` to mark the photo checked and move to the next. A photo with no pigs is still checked: it becomes a "no pig" example.
5. Click **Complete review** when every photo is checked.
- **Tips:** box the whole warm body, not just the brightest spot, and keep the boxes tight. Be consistent: the model learns your boxes.
- **Faster, once the BAMBI model exists:** run AI first, then accept, reject or adjust its boxes instead of drawing them all.

## Step 2: Export (2 min)
**In the app (D-038, preferred):** on each survey page set *AI training data* to Train / Validation / Test, then
Admin › AI training data → Thermal → *Export dataset*. The folder is written to
`C:\AerialWatchData\training-exports\wfp\thermal-<date>-<time>` (use it as `--data` below).

**Fallback (script):**
```powershell
cd <repo>\src\ml; .venv\Scripts\activate
pip install pyodbc
python tools/export_reviews_to_yolo.py --survey-prefix "NACAS" --out C:\NACAS-data\yolo-nacas --val 130221_1000 --test 140221
```
- **Split by flight:**
  - All 14 Feb flights → **test** (about 74 thermal photos, never trained on).
  - 13 Feb 10:00 → **val**.
  - The rest → **train** (about 120 photos).
- The script refuses to write inside the repo. It prints the photo and pig counts per split; paste those counts into the evidence (counts only).

## Step 3: Test the BAMBI model as-is ("before")
```powershell
python tools/evaluate.py --data C:\NACAS-data\yolo-nacas --split test --weights models\yolov8s-bambi-<date>.pt --imgsz <W> --out C:\NACAS-data\results\before
python tools/evaluate.py --data C:\NACAS-data\yolo-nacas --split test --baseline --out C:\NACAS-data\results\baseline
```
`<W>` is the thermal photo width, rounded up to a multiple of 32 (e.g. 640). Get it from the resolution check in 05-nacas-data-rules.md.

## Step 4: Fine-tune locally (CPU about 1–3 h overnight; NVIDIA GPU in minutes)
```powershell
pip install -r requirements-train.txt
python tools/train.py --data C:\NACAS-data\yolo-nacas\data.yaml --model models\yolov8s-bambi-<date>.pt --epochs 40 --imgsz <W> --batch 4 --name nacas
```
The output is `models\…-nacas-<date>.pt`. `models\` is git-ignored, and this file stays local until NACAS approves sharing it.

## Step 5: Test again ("after") and compare
```powershell
python tools/evaluate.py --data C:\NACAS-data\yolo-nacas --split test --weights models\<nacas model>.pt --imgsz <W> --out C:\NACAS-data\results\after
```
Put **only the numbers** in `evidence/<date>-ml/nacas-results.md`, never images:

| Model | Precision | Recall | F1 | AP50 | Count error |
|---|---|---|---|---|---|
| Hotspot baseline | | | | | |
| BAMBI only (Austria) | | | | | |
| BAMBI + NACAS fine-tune | | | | | |

This "Austria → NT" table is the headline AI result for the judges. Credit NACAS.

## Honest limits
- The test set is small: about 74 photos from one day. Say so.
- The labels are ours, not expert-verified. Ask NACAS whether they have per-photo pig counts to check against.
- Thermal only. The RGB photos (about 9 MB each) need tiling and are not planned before submission.
