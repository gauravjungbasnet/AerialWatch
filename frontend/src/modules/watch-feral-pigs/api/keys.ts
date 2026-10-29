import type { CameraType, ReviewImageFilter, SurveyListQuery } from './types';

/**
 * Every TanStack Query key used by Watch Feral Pigs, in one place (docs/18 step 2).
 * All keys start with 'wfp', so `allWfpKeys` refreshes the whole module (used after model coefficients change).
 * Keys are prefixes of each other on purpose: refreshing `reviewKeys.all(id)` also refreshes that survey's twin checks.
 */
export const allWfpKeys = ['wfp'] as const;

export const modelKeys = {
  planner: (camera: string, season: string, date: string) => ['wfp', 'planner', camera, season, date] as const,
  environment: (camera: string, canopy: number, temp: string) => ['wfp', 'environment', camera, canopy, temp] as const,
  info: ['wfp', 'model-info'] as const,
};

export const adminKeys = {
  modelParameters: ['wfp', 'admin', 'model-parameters'] as const,
  history: (take: number) => ['wfp', 'admin', 'model-parameters', 'history', take] as const,
};

export const surveyKeys = {
  all: ['wfp', 'surveys'] as const,
  lists: ['wfp', 'surveys', 'list'] as const,
  list: (query: SurveyListQuery) => ['wfp', 'surveys', 'list', query] as const,
  stats: ['wfp', 'surveys', 'stats'] as const,
  detail: (id: string) => ['wfp', 'surveys', id] as const,
  images: (id: string) => ['wfp', 'surveys', id, 'images'] as const,
};

export const reviewKeys = {
  all: (surveyId: string) => ['wfp', 'review', surveyId] as const,
  summary: (surveyId: string) => ['wfp', 'review', surveyId, 'summary'] as const,
  /** Every queue of the survey, whatever the filter. */
  queues: (surveyId: string) => ['wfp', 'review', surveyId, 'images'] as const,
  images: (surveyId: string, filter: ReviewImageFilter) => ['wfp', 'review', surveyId, 'images', filter] as const,
  /** Every photo detail of the survey. */
  photos: (surveyId: string) => ['wfp', 'review', surveyId, 'image'] as const,
  image: (surveyId: string, imageId: string) => ['wfp', 'review', surveyId, 'image', imageId] as const,
};

export const twinKeys = {
  /** `confirmed` and `boxesLoadedAt` make the twin check refetch only after the server applied a box change. */
  photo: (surveyId: string, imageId: string, confirmed: string, boxesLoadedAt: number) =>
    ['wfp', 'review', surveyId, 'twin', imageId, confirmed, boxesLoadedAt] as const,
  photoAll: (surveyId: string, imageId: string) => ['wfp', 'review', surveyId, 'twin', imageId] as const,
  summary: (surveyId: string) => ['wfp', 'twinSummary', surveyId] as const,
};

export const trainingKeys = {
  all: ['wfp', 'training'] as const,
  overview: (camera: CameraType) => ['wfp', 'training', camera] as const,
};

export const resultsKeys = {
  survey: (surveyId: string) => ['wfp', 'results', surveyId] as const,
  photo: (surveyId: string, imageId: string) => ['wfp', 'results', surveyId, 'photo', imageId] as const,
  photoTwin: (surveyId: string, imageId: string) => ['wfp', 'results', surveyId, 'photoTwin', imageId] as const,
};
