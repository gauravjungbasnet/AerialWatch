// AI training data (D-038). Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { TrainingSplit, TrainingSplitTotals } from '@shared/types/training';
import type { CameraType } from './common';
import type { SurveyStatus } from './surveys';

export interface TrainingSurvey {
  id: string;
  name: string;
  surveyDate: string;
  startTime: string;
  status: SurveyStatus;
  split: TrainingSplit;
  photos: number;
  checkedPhotos: number;
  flaggedPhotos: number;
  usablePhotos: number;
  photosWithPigs: number;
  pigs: number;
}

export interface TrainingOverview {
  camera: CameraType;
  exportRoot: string;
  surveys: TrainingSurvey[];
  totals: TrainingSplitTotals[];
  warnings: string[];
  canExport: boolean;
}

export interface SurveyTrainingSplit {
  surveyId: string;
  split?: TrainingSplit;
}
