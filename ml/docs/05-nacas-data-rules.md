# 11 · NACAS Data Handling (D-037)

> 📁 ML docs live in `src/ml/docs/` (moved from repo `docs/11-nacas-data-handling.md` on 28 Oct 2026). Index: [README.md](README.md).


> Received 26 Oct 2026 from NACAS (CDU): thermal and RGB feral pig imagery.
> Supplier's conditions, quoted: *"must not be shared or uploaded to any third-party public platforms, such as Kaggle or Hugging Face. The data must be stored locally on your computer and used only for local AI model training."*

## Rules we follow
| # | Rule | How |
|---|---|---|
| 1 | Stored only on Gaurav's computer | Folder **outside the git repo**, e.g. `C:\NACAS-data\2026-10-26\` (not `CodingCompetition\data\`, because `src/.gitignore` only covers `src/data/`). No OneDrive/Google Drive/Dropbox sync on that folder. |
| 2 | Never on a public or third-party platform | Not on GitHub, Kaggle, Hugging Face, Roboflow, Google Colab/Drive, YouTube, or any AI chat or upload tool (including Claude). Labelling tools must run locally. |
| 3 | Used only for **local** model training | BAMBI (public, CC BY 4.0) can still be trained on Colab. NACAS fine-tuning and evaluation run on the local machine (`train.py --weights bambi_best.pt`, small model, CPU or local GPU). |
| 4 | Not shown publicly until NACAS agrees | Demo video, screenshots, pitch deck and showcase use BAMBI or synthetic frames. Seeded demo survey uses BAMBI frames. |
| 5 | Outputs | Only aggregate metrics (precision, recall, F1, AP50, count error) are reported, credited to NACAS, unless NACAS approves more. Weights fine-tuned on NACAS data stay local (`*.pt` is git-ignored) until NACAS confirms they can be shared. |
| 6 | After the project | Delete or return as NACAS instructs; record it in `evidence/contact-log.md`. |

## Questions sent back to NACAS
1. May NACAS images or screenshots appear in the demo video and at the 5 Nov showcase?
2. May the fine-tuned model weights and aggregate accuracy figures be included in the submission?
3. Is local-only training required, or is a university-managed GPU acceptable?
4. Preferred credit wording.
5. Delete or return the data after the project?

## Impact on the sprint (docs/10)
- Task 2 unchanged: BAMBI pre-training on Colab.
- New sub-step: fine-tune on NACAS **locally** from the BAMBI weights; evaluate on a held-out NACAS split (split by flight).
- Thermal and RGB are both available: train thermal first (matches BAMBI and the paper's best detection); RGB is a later model.
