// WFP API: one endpoint file per feature in ./endpoints. Pages and hooks keep using `wfpApi`.
import { plannerApi } from './endpoints/planner';
import { surveysApi } from './endpoints/surveys';
import { photosApi } from './endpoints/photos';
import { reviewApi } from './endpoints/review';
import { adminApi } from './endpoints/admin';
import { trainingApi } from './endpoints/training';
import { resultsApi } from './endpoints/results';
import { twinCheckApi } from './endpoints/twinCheck';

export const wfpApi = {
  ...plannerApi,
  ...surveysApi,
  ...photosApi,
  ...reviewApi,
  ...adminApi,
  ...trainingApi,
  ...resultsApi,
  ...twinCheckApi,
};
