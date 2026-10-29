import type {
  CameraType,
  EstimatePreviewRequest,
  EstimateResponse,
  ModelInfo,
  PlannerResult,
  ProbabilityRequest,
  ProbabilityResponse,
  Season,
} from '../types';
import { client } from '../http';

/** WFP planner endpoints (/api/wfp/...). */
export const plannerApi = {
  plannerWindows(camera: CameraType, opts: { season?: Season; date?: string }) {
    const params = new URLSearchParams({ camera });
    if (opts.season) params.set('season', opts.season);
    if (opts.date) params.set('date', opts.date);
    return client.request<PlannerResult>(`/planner/getWindows?${params.toString()}`);
  },

  probability(body: ProbabilityRequest) {
    return client.request<ProbabilityResponse>('/planner/getProbability', {
      method: 'POST',
      body: JSON.stringify(body),
    });
  },

  estimatePreview(body: EstimatePreviewRequest) {
    return client.request<EstimateResponse>('/estimate/preview', { method: 'POST', body: JSON.stringify(body) });
  },

  modelInfo() {
    return client.request<ModelInfo>('/model/info');
  },
};
