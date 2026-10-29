# 10 · ML decisions and changelog

> 📁 ML-related extracts. The full log is in the repo root `docs/00-decision-log.md`.

## Decisions
| ID | Date | Decision |
|---|---|---|
| D-006 | 19 Oct | Build regardless of NT data. The AI is a **swappable slot**; its accuracy is reported separately from the research model. Train on BAMBI, then fine-tune and test on CDU images if shared. Rangers can always draw boxes manually. |
| D-009 | 19 Oct | NACAS will provide feral pig images; NACAS gets free access to the tool, the source and the model. Images stay outside the repo. |
| D-034 | 25 Oct | Review tool: AI boxes are **never deleted, only rejected** (live precision and recall); confirmed = accepted (AI + ranger). |
| D-035 | 25 Oct | ML pipeline: FastAPI service (YOLOv8 + hotspot baseline), BAMBI→YOLO converter (wild boar, split by flight), train/evaluate scripts, Colab notebook. |
| D-037 | 26 Oct | **NACAS data handling:** local only; BAMBI on Colab, NACAS fine-tuning on the local PC; numbers only in evidence ([05](05-nacas-data-rules.md)). |
| D-038 | 27 Oct | **In-app training data export** (shared Common exporter + WFP adapter): tag surveys Train/Val/Test, export YOLO ([07](07-training-data-export.md)). |
| ML-01 | 28 Oct | **One detector per camera.** `/detect?camera=thermal\|rgb`; RGB has no training-free fallback (503), thermal falls back to the hotspot baseline. |
| ML-02 | 28 Oct | **RGB tiling** in the service (1280 px, 20% overlap, inner-edge boxes dropped, NMS). On by default for RGB. |
| ML-03 | 28 Oct | **RGB training uses BAMBI's RGB boxes** (`<id>_rgb_gt.txt`, owl-transferred), never the thermal boxes (the views aren't aligned). |
| ML-04 | 28 Oct | **Model names** `yolov8s-<dataset>-<date>`: `bambi-thermal`, `bambi-rgb`, `nacas-thermal`. The model card records camera, data source, freeze and learning rate. |
| ML-05 | 28 Oct | RGB detector is **experimental** for the competition; NACAS RGB fine-tune is after submission. |

## Changelog
| Date | Change |
|---|---|
| 25 Oct | `src/ml` created: service, baseline, converter, train/evaluate, notebook, 13 tests |
| 26 Oct | 22 BAMBI flights chosen (`evidence/2026-10-26-ml/flight-stats.csv`); stride 1; `bambi_pipeline.ps1` helper; `export_reviews_to_yolo.py` |
| 27 Oct | In-app training data export (D-038) |
| 28 Oct | Per-camera models, tiling, `/models`; converter RGB fix; `train.py` fine-tune options; `evaluate.py --tile`; notebook camera switch; script steps 10–12 (RGB); ML docs moved into `src/ml/docs/`; `src/ml/AGENTS.md` |
