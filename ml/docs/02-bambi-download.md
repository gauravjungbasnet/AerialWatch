# BAMBI Download Guide (teammate, step 1 of the AI work)

> 📁 ML docs live in `src/ml/docs/` (moved from repo `docs/13-bambi-download-guide.md` on 28 Oct 2026). Index: [README.md](README.md).


> **Your task today:** download the BAMBI annotations and videos for **22 wild-boar flights** to your PC.
> It takes about 30 min of setup, then 1–3 h of unattended downloading. Training comes later, in a separate guide.
> Questions or errors: send Gaurav the **step number and the first error line**.

## Why
Our app (Aerial Watch, CDU IT Code Fair) uses AI to suggest boxes around **feral pigs in thermal drone photos**. To
train that AI we use **BAMBI**, a public dataset of drone videos of wildlife from Austria. It includes wild boar,
which is the same species as feral pigs, and the boxes are already drawn. Licence: CC BY 4.0, free to use with credit.

Each **flight** (one drone video) has:
- `<id>_gt.txt`: the annotations (the boxes), a few MB
- `<id>_*.mp4`: the video, about 1 GB

The full dataset is about 350 GB, so we only take the 22 flights picked for training and testing.

## Rules
- **BAMBI only.** You won't get our other dataset (from NACAS at CDU); it stays on Gaurav's PC.
- **Don't upload the videos anywhere** (Drive, GitHub, etc.). They stay on your PC for the next step.

## What you need
- A Windows 10/11 PC with **Git** installed.
- **Python 3.12** from python.org. During install, tick **"Install py launcher"**. Don't use 3.14: the libraries
  don't support it yet.
- About **30 GB free** disk space. Check in File Explorer → This PC.
- A stable internet connection, with the PC kept awake while downloading.

---

## Step 1: Clone the BAMBI tools (5 min)
Open **PowerShell** and run:
```powershell
python --version            # just to see what you have
py -3.12 --version          # must print Python 3.12.x
mkdir C:\aw
cd C:\aw
git clone https://github.com/bambi-eco/Dataset.git bambi-tools
cd bambi-tools
py -3.12 -m venv .venv
.venv\Scripts\python.exe -m pip install --upgrade pip
.venv\Scripts\python.exe -m pip install -r requirements.txt
```
✅ The last command ends without a red error.
Short on space on C:? Use `D:\aw` everywhere instead of `C:\aw`.

## Step 2: Set the flight list (every time you open a new PowerShell)
```powershell
cd C:\aw\bambi-tools
$F = "31 28 41 140 40 142 46 34 96 246 141 135 150 147 146 276 284 282 283 280 277 279".Split()
```
| Use | Flights |
|---|---|
| Train (12) | 31, 28, 41, 140, 40, 142, 46, 34, 96, 246, 141, 135 |
| Validation (3) | 150, 147, 146 |
| Test (7) | 276, 284, 282, 283, 280, 277, 279 |

These follow BAMBI's official split, picked for the most pig frames. Don't change them.

## Step 3: Download the annotations (2 min)
This is quick, and it proves the tool works before the big download.
```powershell
.venv\Scripts\python.exe download_from_zenodo.py -f $F -o C:\aw\bambi --annotations-only
```
✅ `C:\aw\bambi` now contains `31_gt.txt`, `28_gt.txt` and so on, **22 `_gt.txt` files**. Check with:
```powershell
(Get-ChildItem C:\aw\bambi -Recurse -Filter *_gt.txt).Count
```

## Step 4: Check the video size (1 min)
```powershell
.venv\Scripts\python.exe download_from_zenodo.py -f $F -o C:\aw\bambi --dry-run
```
This only lists what will be downloaded. ✅ It's about 20–25 GB. If it's much bigger or you lack space, stop and tell Gaurav.

## Step 5: Download the videos (1–3 h, unattended)
```powershell
.venv\Scripts\python.exe download_from_zenodo.py -f $F -o C:\aw\bambi --unzip
```
- Leave it running and keep the PC awake (Settings → Power → Sleep: Never, for now).
- If it stops halfway, run **Steps 2 and 5 again**. Finished flights are kept.

## Step 6: Verify (1 min)
Paste this whole block into PowerShell:
```powershell
$F = "31 28 41 140 40 142 46 34 96 246 141 135 150 147 146 276 284 282 283 280 277 279".Split()
$bad = @()
foreach ($f in $F) {
  $gt = Get-ChildItem C:\aw\bambi -Recurse -Filter "${f}_gt.txt" | Select-Object -First 1
  if (-not $gt) { $bad += "$f : no annotation"; continue }
  $mp4 = Get-ChildItem $gt.DirectoryName -Filter "${f}*.mp4" | Select-Object -First 1
  if (-not $mp4) { $bad += "$f : no video next to $($gt.Name)" }
}
if ($bad) { "MISSING:"; $bad } else { "OK: all 22 flights have annotation + video side by side" }
"{0:N1} GB downloaded" -f ((Get-ChildItem C:\aw\bambi -Recurse | Measure-Object Length -Sum).Sum / 1GB)
```
✅ It prints `OK: all 22 flights ...`. If anything is missing, run Step 5 again, then this check again.

## Step 7: Tell Gaurav
Send him one message: **"BAMBI download done: OK 22/22, XX GB, in C:\aw\bambi"**. The training guide comes next.

---

## Troubleshooting
| Problem | Fix |
|---|---|
| `py -3.12` not found | Install Python 3.12 from python.org with "py launcher" ticked, then open a **new** PowerShell. |
| `git` not found | Install Git for Windows, then open a new PowerShell. |
| `pip install` fails | Check your internet or VPN/proxy, then re-run the same command. |
| Download is very slow or fails | Zenodo can be slow. Re-run Step 5 later; it continues where it stopped. |
| "No space left" | Free some space, or use `D:\aw` (repeat Step 1 there). |
| Anything else | Send Gaurav the step number and the first error line. |
