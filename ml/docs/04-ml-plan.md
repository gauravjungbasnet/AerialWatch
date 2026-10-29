# 09 · Machine learning plan (pig detector)

> 📁 ML docs live in `src/ml/docs/` (moved from repo `docs/09-machine-learning-plan.md` on 28 Oct 2026). Index: [README.md](README.md).


> Status: v1 · 25 Oct 2026 · Decisions: D-006 (AI is a swappable slot), D-009 (NACAS data), D-034 (review), D-035 (ML pipeline)
> How-to and commands: ../README.md. Code: `src/ml/`.

## 1. What the AI is for (and what it isn't)
- **For:** putting a box on every likely pig so a ranger checks a photo in seconds instead of searching it.
- **Not for:** deciding the count. Only boxes a ranger accepts (or draws) are counted (D-034), and the research model
  corrects that count (D-007). Weak AI makes the review slower but never makes the estimate wrong.
- **Shown to judges as:** precision and recall on flights the model never saw, next to a no-training baseline, and an
  honest note that NT accuracy needs NACAS photos.

## 2. Pipeline
```
BAMBI wild-boar flights ──bambi_to_yolo.py──► YOLO dataset ──train.py (Colab T4)──► yolov8s-bambi-<date>.pt + card
   (video + MOT boxes)       (split by flight)                                              │
                                                                   evaluate.py ◄────────────┤  metrics → evidence/
NACAS photos ──► review tool (rangers draw boxes) ──► export ──► fine-tune ──► …-nacas.pt   │
                                                                                            ▼
Upload ──► .NET API ──► ML service /detect ──► wfp.Detections (Ai, Pending) ──► review tool ──► confirmed count C
```

## 3. Work list
| # | Task | Who / where | Time | Status |
|---|---|---|---|---|
| 1 | Detection service, baseline, converter, train/eval scripts, notebook, tests | Claude (done in repo) | — | ✅ 13 tests pass |
| 2a | Python 3.12 venvs (`data/bambi/Dataset`, `src/ml`); 3.14 lacks wheels | Gaurav | 5 min | ⏳ |
| 2b | `filter_flights.py --species "Sus scrofa" -v` → download **annotations only** for every boar flight to `data/bambi/annotations` | Gaurav | 10 min | ⏳ |
| 2c | `bambi_to_yolo.py --stats` → `flight-stats.csv`; choose ~20 flights (≈14 train, 2 val, 4–5 test from BAMBI's official `splits.json`; varied dates/drones; some with deer) | Gaurav + Claude | 15 min | ⏳ |
| 2d | Download the chosen videos (~1 GB each, ~20 GB total; check disk first) | Gaurav, PC, unattended | 1–3 h | ⏳ |
| 3 | Convert with `--bambi-splits` and check the preview images (boxes on pigs?); cross-check one frame with BAMBI's `mot_frame_viewer.py` | Gaurav, PC | 30 min | ⏳ |
| 4 | Train YOLOv8s, 60 epochs, imgsz 1024 | Colab T4 | 1–2 h unattended | ⏳ |
| 5 | Evaluate YOLO + baseline on test flights; copy metrics + curves to `evidence/<date>-ml/` | Colab | 10 min | ⏳ |
| 6 | .NET `IDetectorClient` + "Run AI" after upload (Processing → InReview); show model version in review | Claude | 0.5 day | next |
| 7 | NACAS photos: label in the review tool, export to YOLO, fine-tune, evaluate on NT photos | when data arrives | 0.5 day | waiting on Prof Campbell |
| 8 | Stretch: active learning export (accepted/rejected boxes → training data), duplicate detection across overlapping photos | later | — | stretch |

## 4. Choices and why
| Choice | Why | Alternative |
|---|---|---|
| YOLOv8s at 1024 px | Small, fast and well documented. 1024 keeps 15–40 px pigs visible | v8n (faster, weaker); v8m (better, slower to train on a T4) |
| Train on BAMBI wild boar, thermal view only | Same species, nadir drone thermal, CC BY 4.0, boxes already drawn | WAID/other RGB sets (different species and sensor) |
| Split by flight, not by frame | Neighbouring frames are near-duplicates, so a frame split inflates the scores | Random frame split (rejected: leakage) |
| Rotations, flips and no colour jitter | A nadir view has no "up". Thermal palettes shouldn't get invented colours | Default augmentations |
| Hotspot baseline | Lets the app work without a GPU and shows what learning adds | No baseline (weaker evaluation story) |
| AI boxes are never deleted, only rejected | Every review becomes evaluation data (precision = accepted / (accepted + rejected); misses = ranger boxes) | Delete on reject |

## 5. What "good enough" looks like
- **Recall ≥ 0.8 at the service threshold** on BAMBI test flights: rangers rarely need to draw a box.
- **Precision ≥ 0.6**: at most about 4 in 10 suggestions rejected (bulk-accept above the best-F1 threshold helps).
- If we miss these, the demo still works (review covers it), and we report the real numbers, not better-sounding ones.

## 6. Risks
| Risk | Mitigation |
|---|---|
| BAMBI download is slow or huge | Annotations first (a few MB); choose flights with many boar boxes; stride 15 |
| Boxes misaligned (frame or view offset) | `--check` previews; `--frame-offset` and `--x-offset` options |
| Domain gap (Austria → NT) | Say so plainly; fine-tune on NACAS photos when they arrive; review step covers misses |
| No GPU time | Colab free T4. If it's unavailable, train v8n for fewer epochs, or demo with the baseline |
| AGPL-3.0 (Ultralytics) | The repo is open source and NACAS gets the source (D-009); listed in licences |

## 7. Decisions from the planning session (26 Oct)
- **Budget ~20 GB → ~20 flights.** Target 3,000–6,000 training images, ≥ 500 test images, ≥ 1,000 test pigs.
- **Use BAMBI's official flight split** (`flight_metadata/splits.json`: 208 train / 48 val / 130 test) so results are
  comparable with other BAMBI work and nothing leaks between train and test.
- **Thermal first.** NACAS camera type is unknown (asked, see contact log). If NACAS photos are RGB, add a second,
  "experimental" RGB model trained on BAMBI `owl-transferred` boxes (`--view rgb`), chosen by the survey's camera.
- If wild boar flights are scarce (< 10), lower the stride to 5 and accept a smaller test set.
