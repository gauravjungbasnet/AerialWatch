# Aerial Watch: BAMBI Training Guide (for teammates)

> 📁 ML docs live in `src/ml/docs/` (moved from repo `docs/12-bambi-training-handoff.md` on 28 Oct 2026). Index: [README.md](README.md).


> Read this once from top to bottom (about 10 minutes), then follow **Part 3** step by step.
> Questions or errors: send Gaurav the **step number and the first error line**.

---

## Part 1: What this is and why

### The project
**Aerial Watch** is our CDU IT Code Fair entry. It helps rangers count **feral pigs from drone photos**:

1. A ranger uploads drone photos of a survey.
2. **AI suggests a box on every likely pig.** That's the part you're building.
3. The ranger accepts or rejects each box in the review tool, or draws boxes the AI missed.
4. The app corrects the confirmed count with the CDU research model (Hvala et al. 2023). Drones miss some pigs under
   trees or in the heat, so **estimated pigs = counted ÷ detection probability**, given with a range.

The AI only **suggests**; a person always confirms. A weaker model makes review slower, but it never makes the
estimate wrong. Still, a good model is a big part of the demo.

### Your job
Train a **YOLOv8** object detector to find pigs in **thermal drone images**, using the public **BAMBI** dataset. Then
send Gaurav the trained model and its accuracy numbers.

| Term | Meaning |
|---|---|
| **BAMBI** | Public drone dataset from Austria: thermal + RGB videos of wildlife, including wild boar (the same species as feral pigs), with boxes already drawn. Licence CC BY 4.0 (free to use with credit). |
| **Flight** | One drone video. Each has an annotation file `<id>_gt.txt` and a video `<id>_*.mp4`. |
| **YOLO / YOLOv8** | A fast object-detection model. We start from a pre-trained `yolov8s` and teach it pigs. |
| **Train / val / test** | Train = what the model learns from. Val = checked during training. Test = flights it **never sees**, used for the honest score. |
| **Colab** | Google's free notebooks with a GPU (T4). Training on a normal laptop would take days; on Colab it's 1–2 hours. |
| **Precision / recall** | Precision = how many AI boxes are really pigs. Recall = how many real pigs the AI found. |

### The pipeline
```
Your PC                                                     Google Colab (GPU)
BAMBI videos + annotations ─► convert to YOLO images ─► zip ─► Google Drive ─► train ─► evaluate ─► results/
        (step 4)                    (step 7)           (step 9)                 (1–2 h)
```

---

## Part 2: Rules (important)

1. **BAMBI data only.** Our other dataset (from NACAS at CDU) is restricted: it stays on Gaurav's PC and must never go
   to Colab, Google Drive, GitHub or any AI tool. You won't receive it, and you don't need it.
2. **Never upload the raw BAMBI videos** (about 20 GB). Only upload the two zips from step 9.
3. **Don't commit data or model files to git** (`*.pt`, images, videos). Send them via the shared Drive folder.
4. **Report real numbers**, even if they're lower than hoped.

---

## Part 3: Do it

### What you need
- A Windows 10/11 PC with **Git** installed.
- **Python 3.12** from python.org. During install, tick **"Install py launcher"**. Don't use Python 3.14: the libraries
  don't support it yet.
- About **30 GB free** disk space.
- A Google account (for Drive and Colab).

### Start the helper script
One PowerShell script does every step on your PC. It shows a menu, and you type a number.

```powershell
git clone <repo-url> AerialWatch
cd AerialWatch
powershell -ExecutionPolicy Bypass -File src\ml\tools\bambi_pipeline.ps1
```
All data goes into `C:\aw`. If C: is short on space, add `-WorkDir D:\aw` to the last command.
You can close the script and start it again at any time; each step can be re-run.

### The steps (do them in order)

**Step 1: Check this PC** · 1 min
Checks for Python 3.12, git, tar and at least 30 GB free, and creates `C:\aw`.
✅ Everything shows OK.

**Step 2: Set up BAMBI tools** · 5 min
Downloads the official BAMBI download tools to `C:\aw\bambi-tools` and installs their Python packages.
✅ "BAMBI tools ready".

**Step 3: Show download size** · 1 min
A dry run that only lists what will be downloaded. It's normally about 20–25 GB. If it's much bigger, stop and tell Gaurav.

**Step 4: Download the 22 flights** · 1–3 h
Downloads the annotations and videos to `C:\aw\bambi`. Leave it running and keep the PC awake.
✅ It finishes without an error.

**Step 5: Verify the download** · 1 min
Checks that every flight has its `_gt.txt` **next to** its `.mp4`.
✅ "All 22 flights have annotations + video side by side". If any are missing, run step 4 again or send the list to Gaurav.

**Step 6: Set up repo ML environment + run tests** · 5 min
Creates `src\ml\.venv` and runs our automated tests.
✅ All tests pass. If they fail, send Gaurav the first error.

**Step 7: Convert to YOLO dataset** · 10–30 min
Cuts the thermal frames out of the videos and writes the images and box labels to `C:\aw\yolo-bambi`.
- It asks for **x-offset** and **frame-offset**. The first time, **press Enter for both** (that means 0).
- ✅ The counts are about **3,900 train, 230 val and 710 test** images.

**Step 8: Open check images** · 2 min
Opens 20 preview images with the label boxes drawn in green. **This is the most important check:** training on wrong
boxes wastes hours.
- ✅ Green boxes sit **on** the pigs (small warm blobs).
- Boxes are shifted sideways by the same amount → run **step 7** again and type `1024` for x-offset.
- Boxes lag slightly behind the pigs → run **step 7** again and type `1` for frame-offset.
- Not sure → send Gaurav 1–2 check images.

**Step 9: Make zips for Google Drive** · 5 min
Creates `ml.zip` (our code, without venv or models) and `yolo-bambi.zip` (the dataset) in `C:\aw\upload-to-drive`.
✅ The folder opens with both zips.

### Train in Colab
1. In Google Drive, create the folder **`MyDrive/AerialWatch/`** and upload **both zips** into it.
2. Go to colab.research.google.com → File → Upload notebook → choose
   `src\ml\notebooks\train_yolo_colab.ipynb` from the repo.
3. Choose Runtime → Change runtime type → **T4 GPU** → Save.
4. Run the cells **from top to bottom** (Shift+Enter). Allow Drive access when asked.

| Cell | What it does |
|---|---|
| GPU check | Shows "Tesla T4". If it shows nothing, the GPU isn't on (see step 3). |
| Mount Drive | Connects your Drive. |
| Unzip + install | Unpacks both zips and installs YOLO (about 2 min). |
| Check labels | Shows 4 preview images. The boxes must be on the pigs, as in step 8. |
| Train | 60 epochs, about **1–2 h**. Keep the browser tab open. |
| Evaluate | Scores the trained model **and** a no-AI baseline on the test flights. |
| Save | Copies everything to `MyDrive/AerialWatch/results/`. |

- **If Colab disconnects:** run the cells again from the top.
- **If no GPU is available** (daily limit): try again later, or edit the Train cell to
  `--model yolov8n.pt --epochs 40` (smaller model, faster).

### Send back to Gaurav
Share the **`MyDrive/AerialWatch/results/`** folder with him. It should contain:
- `yolov8s-bambi-<date>.pt` (the trained model) and its `.json` card
- `metrics-*.md`, two files: YOLO and baseline
- `results.png`, `PR_curve.png`, `confusion_matrix.png`

Also send a one-line message with the **precision, recall and F1** from the YOLO metrics file.

---

## Part 4: Troubleshooting

| Problem | Fix |
|---|---|
| "running scripts is disabled" | Start the script exactly as shown, with `-ExecutionPolicy Bypass`. |
| "Python 3.12 not found" | Install 3.12 from python.org with "py launcher" ticked, open a **new** PowerShell, and start again. |
| `pip install` fails | Check your internet or proxy and re-run the same step. |
| Download stops halfway | Run step 4 again; finished flights are kept. Then run step 5. |
| Step 7 says "annotations but no video" | That flight's video is missing. Run step 4 again, then step 5. |
| Colab "out of memory" | In the Train cell, change `--batch 8` to `--batch 4`. |
| Anything else | Send Gaurav the step number and the first error line. |

## Targets
- **Recall ≥ 0.8** and **precision ≥ 0.6** on the test flights.
- If we miss these, the app still works because rangers review every box. We report the real numbers.

---

## Reference: what was decided and why
- **22 flights, BAMBI's official split** (so nothing leaks between train and test):
  - Train: 31, 28, 41, 140, 40, 142, 46, 34, 96, 246, 141, 135
  - Val: 150, 147, 146
  - Test: 276, 284, 282, 283, 280, 277, 279
  - Picked for the most pig frames, from `evidence/2026-10-26-ml/flight-stats.csv`.
- **Thermal view only:** the paper's best detection is thermal, and BAMBI's boxes are drawn on thermal.
- **`--stride 1`:** BAMBI only labels selected frames (about 100–900 per flight). The default stride 15 would leave
  only about 650 images.
- **Image size 1024:** pigs are small (15–40 px), and smaller images would lose them.
- **Credit:** BAMBI is CC BY 4.0, so it's cited in our README and licences file.
- Background docs in the repo: 04-ml-plan.md (ML plan), 05-nacas-data-rules.md
  (data rules), ../README.md (all commands).
