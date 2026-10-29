import type { CameraType, SurveyTrainingSplit, TrainingOverview } from '../types';
import type { TrainingExportResult, TrainingSplit } from '@shared/types/training';
import { client } from '../http';

/** WFP training endpoints (/api/wfp/...). */
export const trainingApi = {
  trainingOverview(camera: CameraType) {
    return client.request<TrainingOverview>(`/trainingData/overview?camera=${camera}`);
  },

  setTrainingSplit(surveyId: string, split: TrainingSplit | null) {
    return client.request<SurveyTrainingSplit>(`/trainingData/setSplit/${encodeURIComponent(surveyId)}`, {
      method: 'PUT',
      body: JSON.stringify({ split }),
    });
  },

  exportTrainingData(camera: CameraType) {
    return client.request<TrainingExportResult>('/trainingData/export', {
      method: 'POST',
      body: JSON.stringify({ camera }),
    });
  },
};
