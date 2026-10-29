# Aerial Watch - BAMBI training helper (docs/12-bambi-training-handoff.md)
# Run from anywhere:  powershell -ExecutionPolicy Bypass -File src\ml\tools\bambi_pipeline.ps1
# BAMBI (public, CC BY 4.0) ONLY. NACAS data never goes through this script (docs/11).

param([string]$WorkDir = "C:\aw")

$ErrorActionPreference = "Stop"

# ---- fixed choices (docs/12) ----
$Flights = @(31, 28, 41, 140, 40, 142, 46, 34, 96, 246, 141, 135,   # train
             150, 147, 146,                                          # val
             276, 284, 282, 283, 280, 277, 279)                      # test
$MlDir     = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path           # <repo>\src\ml
$SrcDir    = Split-Path $MlDir -Parent                               # <repo>\src
$ToolsDir  = Join-Path $WorkDir "bambi-tools"
$RawDir    = Join-Path $WorkDir "bambi"
$YoloDir   = Join-Path $WorkDir "yolo-bambi"
$YoloRgbDir = Join-Path $WorkDir "yolo-bambi-rgb"
$ZipDir    = Join-Path $WorkDir "upload-to-drive"
$ToolsPy   = Join-Path $ToolsDir ".venv\Scripts\python.exe"
$MlPy      = Join-Path $MlDir ".venv\Scripts\python.exe"

function Say($msg, $color = "Cyan") { Write-Host $msg -ForegroundColor $color }
function Ok($msg)   { Write-Host "OK  $msg" -ForegroundColor Green }
function Fail($msg) { Write-Host "ERROR  $msg" -ForegroundColor Red }

function Need-Python312 {
    try { $v = & py -3.12 --version 2>&1 } catch { $v = "" }
    if ("$v" -notmatch "3\.12") { throw "Python 3.12 not found. Install it from python.org (tick 'py launcher'), then run again." }
}

# ---- 1. check this PC ----
function Step-Check {
    Need-Python312; Ok "Python 3.12"
    if (-not (Get-Command git -ErrorAction SilentlyContinue)) { throw "git not found. Install Git for Windows." }; Ok "git"
    if (-not (Get-Command tar -ErrorAction SilentlyContinue)) { throw "tar not found (needs Windows 10 or newer)." }; Ok "tar"
    $drive = (Split-Path $WorkDir -Qualifier).TrimEnd(":")
    $freeGb = [math]::Round((Get-PSDrive $drive).Free / 1GB, 1)
    if ($freeGb -lt 30) { Fail "Only $freeGb GB free on ${drive}: (need about 30 GB). Free space or run with -WorkDir D:\aw" }
    else { Ok "$freeGb GB free on ${drive}:" }
    New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null; Ok "Work folder $WorkDir"
}

# ---- 2. BAMBI tools ----
function Step-BambiTools {
    Need-Python312
    New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null
    if (-not (Test-Path $ToolsDir)) { git clone https://github.com/bambi-eco/Dataset.git $ToolsDir }
    else { Say "bambi-tools already cloned" }
    if (-not (Test-Path $ToolsPy)) { py -3.12 -m venv (Join-Path $ToolsDir ".venv") }
    & $ToolsPy -m pip install -q --upgrade pip
    & $ToolsPy -m pip install -q -r (Join-Path $ToolsDir "requirements.txt")
    Ok "BAMBI tools ready"
}

# ---- 3. size check, 4. download ----
function Invoke-Download([switch]$DryRun, [switch]$RgbBoxes) {
    if (-not (Test-Path $ToolsPy)) { throw "Run step 2 first." }
    New-Item -ItemType Directory -Force -Path $RawDir | Out-Null
    Push-Location $ToolsDir
    try {
        $dlArgs = @("download_from_zenodo.py", "-f") + ($Flights | ForEach-Object { "$_" }) + @("-o", $RawDir)
        if ($RgbBoxes) { $dlArgs += @("--version", "owl-transferred", "--annotations-only") }
        elseif ($DryRun) { $dlArgs += "--dry-run" } else { $dlArgs += "--unzip" }
        & $ToolsPy @dlArgs
        if ($LASTEXITCODE -ne 0) { throw "download_from_zenodo.py exited with $LASTEXITCODE" }
    } finally { Pop-Location }
}

# ---- 5. verify the download ----
function Step-Verify {
    $missing = @()
    foreach ($f in $Flights) {
        $gt = Get-ChildItem $RawDir -Recurse -Filter "${f}_gt.txt" -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $gt) { $missing += "$f (no ${f}_gt.txt)"; continue }
        $mp4 = Get-ChildItem $gt.DirectoryName -Filter "${f}_*.mp4" -ErrorAction SilentlyContinue | Select-Object -First 1
        if (-not $mp4) { $mp4 = Get-ChildItem $gt.DirectoryName -Filter "${f}*.mp4" -ErrorAction SilentlyContinue | Select-Object -First 1 }
        if (-not $mp4) { $missing += "$f (no video next to $($gt.FullName))" }
    }
    if ($missing.Count -eq 0) { Ok "All $($Flights.Count) flights have annotations + video side by side" }
    else { Fail "Missing:"; $missing | ForEach-Object { Write-Host "   $_" -ForegroundColor Yellow }; Say "Re-run step 4 for these, or send this list to Gaurav." "Yellow" }
}

# ---- 10. RGB boxes check ----
function Step-VerifyRgb {
    $have = @($Flights | Where-Object { Get-ChildItem $RawDir -Recurse -Filter "$($_)_rgb_gt.txt" -ErrorAction SilentlyContinue | Select-Object -First 1 })
    Ok "$($have.Count) of $($Flights.Count) flights have RGB boxes (<id>_rgb_gt.txt)"
    if ($have.Count -lt $Flights.Count) { Say "Flights without RGB boxes are skipped for the RGB model; that's expected." "Yellow" }
}

# ---- 6. repo ML environment + tests ----
function Step-MlEnv {
    Need-Python312
    if (-not (Test-Path $MlPy)) { py -3.12 -m venv (Join-Path $MlDir ".venv") }
    & $MlPy -m pip install -q --upgrade pip
    & $MlPy -m pip install -q -r (Join-Path $MlDir "requirements-dev.txt")
    Push-Location $MlDir
    try { & $MlPy -m pytest -q tests; if ($LASTEXITCODE -ne 0) { throw "tests failed (send Gaurav the first error)" } }
    finally { Pop-Location }
    Ok "ML environment ready, tests pass"
}

# ---- 7. convert to YOLO ----
function Step-Convert([string]$View = "thermal") {
    $Out = if ($View -eq "rgb") { $YoloRgbDir } else { $YoloDir }
    if (-not (Test-Path $MlPy)) { throw "Run step 6 first." }
    $xo = Read-Host "x-offset (Enter = 0; use 1024 only if boxes were shifted sideways last time)"
    $fo = Read-Host "frame-offset (Enter = 0; use 1 only if boxes were one frame behind last time)"
    if (-not $xo) { $xo = "0" }; if (-not $fo) { $fo = "0" }
    if (Test-Path $Out) { Remove-Item -Recurse -Force $Out }
    Push-Location $MlDir
    try {
        & $MlPy tools/bambi_to_yolo.py --bambi-root $RawDir --out $Out --view $View --stride 1 --check 20 `
            --bambi-splits (Join-Path $ToolsDir "flight_metadata\splits.json") `
            --flights ($Flights -join ",") --x-offset $xo --frame-offset $fo
        if ($LASTEXITCODE -ne 0) { throw "bambi_to_yolo.py exited with $LASTEXITCODE" }
    } finally { Pop-Location }
    Ok "$View dataset written to $Out"
    foreach ($s in "train", "val", "test") {
        $n = (Get-ChildItem (Join-Path $Out "images\$s") -File -ErrorAction SilentlyContinue).Count
        Write-Host ("   {0,-5} {1} images" -f $s, $n)
    }
    Say "Now open the check images (step 8 thermal, step 12 RGB)."
}

# ---- 8. open the check images ----
function Step-OpenChecks([string]$Dir = $YoloDir) {
    $check = Join-Path $Dir "check"
    if (-not (Test-Path $check)) { throw "No check images. Run step 7 first." }
    Invoke-Item $check
    Say "Green boxes must sit ON the pigs." "Yellow"
    Say "  Shifted sideways      -> convert again with x-offset 1024 (thermal: step 7, RGB: step 11)"
    Say "  One frame behind      -> convert again with frame-offset 1"
    Say "  Not sure              -> send Gaurav one check image"
}

# ---- 9. zips for Google Drive ----
function Step-Zip {
    if (-not (Test-Path $YoloDir)) { throw "Run step 7 first." }
    New-Item -ItemType Directory -Force -Path $ZipDir | Out-Null
    $mlZip = Join-Path $ZipDir "ml.zip"; $yoloZip = Join-Path $ZipDir "yolo-bambi.zip"
    Remove-Item $mlZip, $yoloZip -ErrorAction SilentlyContinue
    tar -a -c -f $mlZip --exclude ".venv" --exclude "__pycache__" --exclude ".pytest_cache" --exclude "*.pt" -C $SrcDir ml
    tar -a -c -f $yoloZip -C $WorkDir yolo-bambi
    if (Test-Path $YoloRgbDir) {
        $rgbZip = Join-Path $ZipDir "yolo-bambi-rgb.zip"; Remove-Item $rgbZip -ErrorAction SilentlyContinue
        tar -a -c -f $rgbZip -C $WorkDir yolo-bambi-rgb
        Ok ("yolo-bambi-rgb.zip {0:N0} MB" -f ((Get-Item $rgbZip).Length / 1MB))
    }
    Ok ("ml.zip {0:N0} MB, yolo-bambi.zip {1:N0} MB" -f ((Get-Item $mlZip).Length / 1MB), ((Get-Item $yoloZip).Length / 1MB))
    Invoke-Item $ZipDir
    Say "Upload the zips to Google Drive -> MyDrive/AerialWatch/ . Never upload the raw videos." "Yellow"
    Say "Then open src/ml/notebooks/train_yolo_colab.ipynb in Colab (T4 GPU) and run all cells."
}

# ---- menu ----
$menu = @(
    @{ n = "1"; t = "Check this PC (Python 3.12, git, disk space)";       a = { Step-Check } },
    @{ n = "2"; t = "Set up BAMBI tools (clone + install)";               a = { Step-BambiTools } },
    @{ n = "3"; t = "Show download size (dry run)";                       a = { Invoke-Download -DryRun } },
    @{ n = "4"; t = "Download the 22 flights (1-3 h, leave it running)";  a = { Invoke-Download } },
    @{ n = "5"; t = "Verify the download";                                a = { Step-Verify } },
    @{ n = "6"; t = "Set up repo ML environment + run tests";             a = { Step-MlEnv } },
    @{ n = "7"; t = "Convert to YOLO dataset (thermal)";                  a = { Step-Convert -View "thermal" } },
    @{ n = "8"; t = "Open check images (boxes on pigs?)";                 a = { Step-OpenChecks } },
    @{ n = "9"; t = "Make zips for Google Drive (thermal + RGB if built)"; a = { Step-Zip } },
    @{ n = "10"; t = "RGB: download RGB boxes (owl-transferred, small)";  a = { Invoke-Download -RgbBoxes; Step-VerifyRgb } },
    @{ n = "11"; t = "RGB: convert to YOLO dataset (yolo-bambi-rgb)";    a = { Step-Convert -View "rgb" } },
    @{ n = "12"; t = "RGB: open check images";                           a = { Step-OpenChecks -Dir $YoloRgbDir } }
)

while ($true) {
    Write-Host ""
    Say "=== Aerial Watch: BAMBI training helper  (work folder: $WorkDir) ==="
    $menu | ForEach-Object { Write-Host ("  {0}. {1}" -f $_.n, $_.t) }
    Write-Host "  Q. Quit"
    $choice = (Read-Host "Choose a number").Trim()
    if ($choice -match "^[qQ]$") { break }
    $item = $menu | Where-Object { $_.n -eq $choice }
    if (-not $item) { Fail "Pick 1-12 or Q"; continue }
    try { & $item.a; Ok "Step $choice done" }
    catch { Fail "Step $choice failed: $($_.Exception.Message)"; Say "Send Gaurav: step $choice + this error." "Yellow" }
}
