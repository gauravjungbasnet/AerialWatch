# Aerial Watch ML: documentation index

Everything about the pig detector lives here, in `src/ml/docs/`. Code: `src/ml/` (see [../README.md](../README.md)).
Rules for AI coding agents: [../AGENTS.md](../AGENTS.md).

## Start here (by role)
| You are… | Read, in order |
|---|---|
| **Teammate training the models** | [01-ml-handoff](01-ml-handoff.md) → [02-bambi-download](02-bambi-download.md) → [03-bambi-training](03-bambi-training.md) |
| **Gaurav (NACAS data and app)** | [05-nacas-data-rules](05-nacas-data-rules.md) → [06-nacas-label-train-test](06-nacas-label-train-test.md) → [07-training-data-export](07-training-data-export.md) → [08-service-api-and-integration](08-service-api-and-integration.md) |
| **New to ML / need a concept** | [09-knowledge-base](09-knowledge-base.md) |
| **Why is it built this way?** | [04-ml-plan](04-ml-plan.md), [10-decisions-and-changelog](10-decisions-and-changelog.md) |

## All documents
| # | Document | What it covers |
|---|---|---|
| 01 | [ml-handoff](01-ml-handoff.md) | **Master plan:** parts A (BAMBI thermal), B (BAMBI RGB), C (NACAS fine-tune), D (run the service), E (connect to .NET and UI), timeline |
| 02 | [bambi-download](02-bambi-download.md) | Download BAMBI annotations and videos for the 22 chosen flights (no repo needed) |
| 03 | [bambi-training](03-bambi-training.md) | Helper script `tools/bambi_pipeline.ps1` steps 1–12, Colab training, what to send back |
| 04 | [ml-plan](04-ml-plan.md) | Original ML plan: pipeline, choices and why, risks, "good enough" targets |
| 05 | [nacas-data-rules](05-nacas-data-rules.md) | NACAS conditions (local only), inventory of what was shared, labelling plan |
| 06 | [nacas-label-train-test](06-nacas-label-train-test.md) | Label NACAS in the app, export, evaluate before, fine-tune, evaluate after |
| 07 | [training-data-export](07-training-data-export.md) | The in-app "AI training data" feature: tag surveys Train/Val/Test, export YOLO |
| 08 | [service-api-and-integration](08-service-api-and-integration.md) | Detection service API (endpoints, JSON, env vars) and how .NET and the frontend use it |
| 09 | [knowledge-base](09-knowledge-base.md) | Concepts and FAQ: YOLO format, splits and leakage, metrics, thermal vs RGB, tiling, BAMBI and NACAS facts |
| 10 | [decisions-and-changelog](10-decisions-and-changelog.md) | ML-related decisions (D-006, D-009, D-035, D-037, D-038) and what changed when |

## Related (repo root, not ML-only)
- `docs/00-decision-log.md`: all decisions
- `docs/03-legal-ethics-attribution.md`: licences and attribution
- `docs/08-database-schema.md`: `wfp.Detections`, `Surveys.TrainingSplit`
- `docs/15-training-data-feature-plan.md`: full build plan of the training-data feature
- `evidence/`: metrics and test results (numbers only for NACAS)
