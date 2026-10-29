// Shared AI-training-data types (D-038). Used by every module; no module imports here.

export type TrainingSplit = 'train' | 'validation' | 'test';

export const TRAINING_SPLITS: TrainingSplit[] = ['train', 'validation', 'test'];

export const trainingSplitLabel: Record<TrainingSplit, string> = {
  train: 'Train',
  validation: 'Validation',
  test: 'Test',
};

/** Totals for one set. photos = usable photos (checked, not flagged); labels = boxes written. */
export interface TrainingSplitTotals {
  split: TrainingSplit;
  sessions: number;
  photos: number;
  photosWithLabels: number;
  labels: number;
}

export interface TrainingExportResult {
  datasetName: string;
  folder: string;
  totals: TrainingSplitTotals[];
  warnings: string[];
  createdAt: string;
}
