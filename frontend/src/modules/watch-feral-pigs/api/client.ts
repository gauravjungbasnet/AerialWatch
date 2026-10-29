import type {
  CameraType,
  CoefficientEntry,
  ModelParametersHistoryItem,
  ModelParametersResponse,
  CreateSurveyRequest,
  SurveyDetail,
  PagedResult,
  SurveyImage,
  SurveyListQuery,
  SurveyStats,
  SurveySummary,
  UploadImagesResponse,
  BoxRect,
  BulkAcceptResponse,
  Detection,
  ReviewImage,
  ReviewImageDetail,
  ReviewImageFilter,
  ReviewSummary,
  UpdateDetectionRequest,
  EstimatePreviewRequest,
  EstimateResponse,
  ModelInfo,
  PlannerResult,
  ProbabilityRequest,
  ProbabilityResponse,
  Season,
  SurveyTrainingSplit,
  TrainingOverview,
  SurveyResults,
  TwinCheck,
  TwinMark,
  SetTwinMarkRequest,
  TwinCheckSummary,
  TwinSighting,
} from './types';
import type { TrainingExportResult, TrainingSplit } from '@shared/types/training';

import { createApiClient } from '@shared/api/client';

const client = createApiClient('/api/wfp');

export const wfpApi = {
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

  // Surveys
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

  // Survey photos (D-030)
  listSurveyImages(surveyId: string) {
    return client.request<SurveyImage[]>(`/surveyImages/list/${encodeURIComponent(surveyId)}`);
  },

  /** One batch (max 20 files) as multipart/form-data; the API answers per file. */
  uploadSurveyImages(surveyId: string, files: File[], signal?: AbortSignal) {
    const form = new FormData();
    for (const file of files) form.append('files', file, file.name);
    return client.request<UploadImagesResponse>(`/surveyImages/upload/${encodeURIComponent(surveyId)}`, {
      method: 'POST',
      body: form,
      signal,
    });
  },

  deleteSurveyImage(id: string) {
    return client.request<void>(`/surveyImages/delete/${encodeURIComponent(id)}`, { method: 'DELETE' });
  },

  surveyImageUrl(id: string) {
    return client.url(`/surveyImages/original/${encodeURIComponent(id)}`);
  },

  surveyThumbnailUrl(id: string) {
    return client.url(`/surveyImages/thumbnail/${encodeURIComponent(id)}`);
  },

  // Review (D-034)
  startReview(surveyId: string) {
    return client.request<ReviewSummary>(`/review/start/${encodeURIComponent(surveyId)}`, { method: 'POST' });
  },

  reviewSummary(surveyId: string) {
    return client.request<ReviewSummary>(`/review/summary/${encodeURIComponent(surveyId)}`);
  },

  completeReview(surveyId: string) {
    return client.request<ReviewSummary>(`/review/complete/${encodeURIComponent(surveyId)}`, { method: 'POST' });
  },

  reopenReview(surveyId: string) {
    return client.request<ReviewSummary>(`/review/reopen/${encodeURIComponent(surveyId)}`, { method: 'POST' });
  },

  bulkAccept(surveyId: string, minConfidence: number) {
    return client.request<BulkAcceptResponse>(`/review/bulkAccept/${encodeURIComponent(surveyId)}`, {
      method: 'POST',
      body: JSON.stringify({ minConfidence }),
    });
  },

  reviewImages(surveyId: string, filter: ReviewImageFilter = 'all') {
    return client.request<ReviewImage[]>(`/review/images/${encodeURIComponent(surveyId)}?filter=${filter}`);
  },

  reviewImage(imageId: string) {
    return client.request<ReviewImageDetail>(`/review/image/${encodeURIComponent(imageId)}`);
  },

  completeImage(imageId: string) {
    return client.request<ReviewImage>(`/review/completeImage/${encodeURIComponent(imageId)}`, { method: 'POST' });
  },

  reopenImage(imageId: string) {
    return client.request<ReviewImage>(`/review/reopenImage/${encodeURIComponent(imageId)}`, { method: 'POST' });
  },

  flagImage(imageId: string, flagged: boolean) {
    return client.request<ReviewImage>(`/review/flagImage/${encodeURIComponent(imageId)}`, {
      method: 'PUT',
      body: JSON.stringify({ flagged }),
    });
  },

  addDetection(imageId: string, box: BoxRect) {
    return client.request<Detection>(`/review/addDetection/${encodeURIComponent(imageId)}`, {
      method: 'POST',
      body: JSON.stringify(box),
    });
  },

  updateDetection(id: string, body: UpdateDetectionRequest) {
    return client.request<Detection>(`/review/updateDetection/${encodeURIComponent(id)}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  },

  deleteDetection(id: string) {
    return client.request<void>(`/review/deleteDetection/${encodeURIComponent(id)}`, { method: 'DELETE' });
  },

  // Admin
  getModelParameters() {
    return client.request<ModelParametersResponse>('/modelParameters/get');
  },

  updateModelParameters(values: CoefficientEntry[], updatedBy?: string) {
    return client.request<ModelParametersResponse>('/modelParameters/update', {
      method: 'PUT',
      body: JSON.stringify({ values, updatedBy }),
    });
  },

  modelParametersHistory(take = 20) {
    return client.request<ModelParametersHistoryItem[]>(`/modelParameters/history?take=${take}`);
  },

  resetModelParameters() {
    return client.request<ModelParametersResponse>('/modelParameters/reset', { method: 'POST' });
  },

  // AI training data (D-038)
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

  // Results + map (Slice 5)
  surveyResults(surveyId: string) {
    return client.request<SurveyResults>(`/results/get/${encodeURIComponent(surveyId)}`);
  },

  // Twin check (D-040)
  twinCheck(imageId: string) {
    return client.request<TwinCheck>(`/twinCheck/get/${encodeURIComponent(imageId)}`);
  },

  setTwinMark(detectionId: string, body: SetTwinMarkRequest) {
    return client.request<TwinMark>(`/twinCheck/set/${encodeURIComponent(detectionId)}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  },

  clearTwinMark(detectionId: string) {
    return client.request<void>(`/twinCheck/clear/${encodeURIComponent(detectionId)}`, { method: 'DELETE' });
  },

  addTwinSighting(imageId: string, box: BoxRect) {
    return client.request<TwinSighting>(`/twinCheck/addSighting/${encodeURIComponent(imageId)}`, {
      method: 'POST',
      body: JSON.stringify(box),
    });
  },

  confirmTwinSighting(markId: string, box: BoxRect) {
    return client.request<TwinMark>(`/twinCheck/confirmSighting/${encodeURIComponent(markId)}`, {
      method: 'POST',
      body: JSON.stringify(box),
    });
  },

  setTwinSightingVisibility(markId: string, notVisibleOnCounted: boolean) {
    return client.request<TwinSighting>(`/twinCheck/sightingVisibility/${encodeURIComponent(markId)}`, {
      method: 'PUT',
      body: JSON.stringify({ notVisibleOnCounted }),
    });
  },

  removeTwinSighting(markId: string) {
    return client.request<void>(`/twinCheck/removeSighting/${encodeURIComponent(markId)}`, { method: 'DELETE' });
  },

  twinCheckSummary(surveyId: string) {
    return client.request<TwinCheckSummary>(`/twinCheck/summary/${encodeURIComponent(surveyId)}`);
  },

  deleteSurvey(id: string) {
    return client.request<void>(`/surveys/delete/${encodeURIComponent(id)}`, { method: 'DELETE' });
  },
};
