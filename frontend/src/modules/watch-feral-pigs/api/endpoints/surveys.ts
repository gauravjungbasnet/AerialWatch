import type {
  CreateSurveyRequest,
  PagedResult,
  SurveyDetail,
  SurveyListQuery,
  SurveyStats,
  SurveySummary,
} from '../types';
import { client } from '../http';

/** WFP surveys endpoints (/api/wfp/...). */
export const surveysApi = {
  /** Server-side filtered, sorted, paged list. Empty values are not sent. */
  listSurveys(query: SurveyListQuery = {}) {
    const params = new URLSearchParams();
    for (const [key, value] of Object.entries(query))
      if (value !== undefined && value !== '') params.set(key, String(value));
    const qs = params.toString();
    return client.request<PagedResult<SurveySummary>>(`/surveys/list${qs ? `?${qs}` : ''}`);
  },

  surveyStats() {
    return client.request<SurveyStats>('/surveys/stats');
  },

  getSurvey(id: string) {
    return client.request<SurveyDetail>(`/surveys/get/${encodeURIComponent(id)}`);
  },

  createSurvey(body: CreateSurveyRequest) {
    return client.request<SurveyDetail>('/surveys/create', { method: 'POST', body: JSON.stringify(body) });
  },

  deleteSurvey(id: string) {
    return client.request<void>(`/surveys/delete/${encodeURIComponent(id)}`, { method: 'DELETE' });
  },
};
