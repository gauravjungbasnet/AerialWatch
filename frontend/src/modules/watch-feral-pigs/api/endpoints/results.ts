import type { SurveyResults } from '../types';
import { client } from '../http';

/** WFP results endpoints (/api/wfp/...). */
export const resultsApi = {
  surveyResults(surveyId: string) {
    return client.request<SurveyResults>(`/results/get/${encodeURIComponent(surveyId)}`);
  },
};
