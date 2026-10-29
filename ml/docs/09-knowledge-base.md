# 09 · ML knowledge base and FAQ

> 📁 Plain-language answers to the questions that came up while building the pig detector. Index: [README.md](README.md).

## The idea in one paragraph
A ranger flies a drone and gets hundreds of photos. **YOLO** (an object detector) suggests a box on every likely pig.
The ranger accepts, rejects or draws boxes in the review tool. Only **confirmed** boxes are counted, and the CDU research
model (Hvala et al. 2023) corrects that count for pigs the drone can't see: **N̂ = C ÷ p**, with a range. So a weak AI
slows the review down but never makes the estimate wrong.

## Glossary
| Term | Meaning |
|---|---|
| **YOLO / YOLOv8** | Fast object detector (Ultralytics, AGPL-3.0). We start from pre-trained `yolov8s` and teach it "pig". |
| **Label / box** | Rectangle around one pig. In the app: pixels (x, y, width, height), top-left origin. |
| **YOLO label line** | `class xc yc w h`, all fractions of the photo size. Example: `0 0.125000 0.312500 0.050000 0.125000` |
| **Train / validation / test** | Learn from / check during training / final honest score on photos never seen |
| **Epoch** | One pass over all training photos (we use 40–60) |
| **Fine-tuning** | Continue training an existing model (BAMBI) on new data (NACAS) with a small learning rate and frozen early layers |
| **Weights (`.pt`)** | The trained model file. Git-ignored. Each comes with a `.json` model card |
| **Confidence** | The model's score 0–1 for a box. Boxes below `AW_MIN_CONFIDENCE` are not shown |
| **Hotspot baseline** | Training-free thermal detector (warm blobs of pig size). Used before a model exists, and as the comparison |
| **BAMBI** | Public Austrian drone dataset with thermal and RGB video of wildlife, including wild boar (same species). CC BY 4.0 |
| **NACAS** | CDU's North Australian Centre for Autonomous Systems. Shared NT feral pig photos, **local only** |

## Splits and leakage
- **Split by flight (survey), never by photo.** Neighbouring drone photos and video frames overlap and show the same
  pigs. If photo 5 is in train and photo 6 is in test, the model is tested on pigs it has already seen, and the score
  looks better than it really is (**data leakage**).
- **Keep a whole day for testing** where possible (NACAS: all 14 Feb flights). This also tests a different day's
  weather and light.
- **Thermal/RGB pairs of the same moment stay in the same set.** This is automatic, because both come from the same flight.

## Why empty label files matter
A checked photo with **no pigs** is exported with an empty `.txt`. It teaches the model "warm rock / log / ground ≠
pig". Without such photos the model learns that every photo contains a pig, and false alarms go up.

## Metrics (from `tools/evaluate.py`, IoU ≥ 0.5)
| Metric | Question it answers | Target |
|---|---|---|
| **Precision** | Of the AI's boxes, how many are real pigs? (low = rangers reject a lot) | ≥ 0.6 |
| **Recall** | Of the real pigs, how many did the AI find? (low = rangers draw a lot) | ≥ 0.8 |
| **F1** | Balance of the two | |
| **AP50** | Quality across all thresholds | |
| **Count error / photo** | AI count minus true count before review | Close to 0 |
| **Best-F1 threshold** | Confidence cut-off with the best F1 | Use it as `AW_MIN_CONFIDENCE` |

Always report YOLO **next to the hotspot baseline**, and BAMBI-only **next to** BAMBI + NACAS. The comparison is the story.

## Thermal vs RGB
| | Thermal (`*_THM.JPG`, about 0.7 MB) | RGB (colour, about 9 MB, about 20 MP) |
|---|---|---|
| What a pig looks like | Bright warm blob on a cooler background | Brown/grey shape that blends into soil and shadow, often hidden under trees |
| Detection (research) | Higher | Lower (the research model uses a lower *p* for RGB) |
| Our model | Main model; fine-tuned on NACAS | **Experimental**, BAMBI-RGB only before submission |
| Tricks | — | **Tiling** (1280 px tiles, 20% overlap, merge duplicates), labelling guided by the thermal pair |
| If no model | Hotspot baseline | Manual review (the service answers 503) |

**Can YOLO learn RGB pigs if people can hardly see them?** Only as well as the labels allow: a model can't be better
than the boxes it learns from. Label RGB using the thermal partner photo as a guide (same file name without `_THM`),
and expect lower accuracy.

**Why tiling?** YOLO shrinks every photo to about 1024 px. A 5,000 px wide RGB photo shrinks five times, so a 40 px pig
becomes 8 px and is lost. On 1280 px tiles the pig keeps its size. Boxes cut by an inner tile edge are dropped,
because the overlap guarantees the whole pig appears in a neighbouring tile, and duplicates are merged.

## BAMBI facts we rely on
- Each flight is one video with **thermal on the left and RGB on the right** (1024 × 1024 each), plus `<id>_gt.txt`
  (thermal boxes). RGB boxes are separate: `<id>_rgb_gt.txt` (`--version owl-transferred`).
- Wild boar = `class_id` 60–69. Other animals are left unlabelled, so deer act as hard negatives.
- 127 flights contain boar. We use **22**, chosen by the most pig frames, following BAMBI's official split:
  - **Train:** 31, 28, 41, 140, 40, 142, 46, 34, 96, 246, 141, 135
  - **Val:** 150, 147, 146
  - **Test:** 276, 284, 282, 283, 280, 277, 279
  - Stats: `evidence/2026-10-26-ml/flight-stats.csv`
- **`--stride 1`:** BAMBI only annotates selected frames (about 100–900 per flight). The default stride 15 would leave only about 650 images.
- Expected thermal dataset: about 3,900 train, 230 val and 710 test images.

## NACAS facts we rely on (names and sizes only; images never opened by AI tools)
- 445 JPGs, 2.3 GB, **no labels**: 204 thermal and 241 RGB, with 170 thermal/RGB pairs (dual camera, same moment).
- 12 flights on 12–14 Feb 2021 at 06:00, 10:00, 14:00 and 18:00, the same time windows as the research design.
  The camera's flight number is in the file name (`IX-12-37642_00NN_…`).
- The two top folders (`120221_1400_nodetections`, `CULL_1800`) are just packaging, with **no duplicates**.
  Empty subfolders are probably `_noflight` (no flight) and `_nodetections` (no pigs seen). `CULL_*` are probably cull-related flights. Confirm with NACAS.
- Suggested split (thermal photos):
  - **Test:** 14 Feb ×4 (about 74 photos)
  - **Validation:** 13 Feb 10:00 (about 10)
  - **Train:** the rest (about 120)
- Rules: [05-nacas-data-rules](05-nacas-data-rules.md). Never on Colab, Drive, GitHub or any AI tool; numbers only in evidence.

## FAQ
- **Do I select one photo or many when uploading?** Many: the whole flight folder (`_THM` files only for the thermal survey). The uploader sends batches of 20.
- **How do I put some photos in test and some in training?** By survey: one survey per flight, and set the survey's training set. Never split one flight.
- **Do we need both BAMBI and NACAS?** Yes. BAMBI gives thousands of labelled examples (learn "pig from the air"); NACAS teaches NT conditions and gives the honest NT test score.
- **Can the teammate train on NACAS in Colab?** No. NACAS fine-tuning runs on Gaurav's PC (CPU overnight is fine for about 120 photos).
- **What is `_THM.JPG`?** The thermal image of a dual-camera shot. Its partner without `_THM` is the RGB photo.
- **What does the app store when I draw a box?** The photo stays unchanged on disk; the box is a row in `wfp.Detections` (pixels, status, who and when).
- **What are thumbs?** About 400 px preview JPEGs for the gallery. Never used for training.
- **Where do exports go?** `C:\AerialWatchData\training-exports\wfp\<camera>-<date>-<time>`. A new folder every time.
