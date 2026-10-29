# 07 · In-app training data export (D-038)

> 📁 Summary for ML work. The full build plan (backend and frontend files, rules R1–R9, tests) is in the repo root
> `docs/15-training-data-feature-plan.md`. Evidence: `evidence/2026-10-27-training-data/`.

## What it does
Turns photos reviewed in Aerial Watch into a YOLO dataset on the local disk, with no scripts.

```
Survey page → "AI training data" card: Not used | Train | Validation | Test   (wfp.Surveys.TrainingSplit)
Admin › AI training data → Thermal | RGB → totals per set, warnings, surveys table → [Export dataset]
   → C:\AerialWatchData\training-exports\wfp\<camera>-yyyyMMdd-HHmmss\
        images\{train,val,test}\*.jpg   labels\{train,val,test}\*.txt   data.yaml   dataset-card.json
```

## Rules enforced in code
| Rule | Meaning for training |
|---|---|
| Split per **survey (flight)** | No leakage between train and test ([09-knowledge-base](09-knowledge-base.md#splits-and-leakage)) |
| Only **checked, unflagged** photos | Unchecked photos may hide unlabelled pigs |
| Only **Accepted** `pig` boxes | Rejected and pending AI boxes are ignored |
| No-pig photo → **empty** `.txt` | Teaches "nothing here" (fewer false alarms) |
| **One camera** per export | Thermal and RGB models are trained separately |
| Every set needs at least one photo | Otherwise export is refused (400) |
| New folder each time, built in `.partial` | Never overwrites a previous dataset |
| Photos without size/file are skipped with a warning | Listed in the result and in `dataset-card.json` |

`data.yaml` has **no `path:` line**, so YOLO resolves the folders next to it. Pass the folder straight to the tools:

```powershell
python tools/evaluate.py --data <export folder> --split test --weights <model>.pt --out <results>
python tools/train.py    --data <export folder>\data.yaml --model <bambi model>.pt ...
```

## Code map
- Shared (all modules):
  - `Domain/Common/Training/TrainingSplit`
  - `Application/Common/Training/` (`TrainingDatasetExporter`, `YoloLabels`, `ITrainingDatasetWriter`)
  - `Infrastructure/Common/Training/LocalTrainingDatasetWriter`
  - `frontend/src/shared/components/training/`
- WFP adapter:
  - `Application/Wfp/Training/TrainingDataService`
  - `Infrastructure/Persistence/Wfp/Repositories/TrainingDataRepository`
  - `Api/Controllers/Wfp/TrainingDataController`
  - `frontend/.../features/training/`, `features/surveys/TrainingSplitCard.tsx`
- Tests: `UnitTests/Common/Training/YoloLabelsTests`, `UnitTests/Wfp/Training/TrainingDataServiceTests`
- Fallback without the app: `tools/export_reviews_to_yolo.py` (reads SQL directly).
