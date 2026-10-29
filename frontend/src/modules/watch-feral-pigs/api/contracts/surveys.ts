// Surveys: list, detail, create. Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { TrainingSplit } from '@shared/types/training';
import type { Assumption, CameraType, Season, TimeWindow } from './common';

export type GroundCover = 'open' | 'mixed' | 'dense' | 'unknown';
export type SurveyStatus = 'draft' | 'uploading' | 'processing' | 'inReview' | 'reviewed' | 'failed';

export interface CreateSurveyRequest {
  name: string;
  siteName: string;
  surveyDate: string; // yyyy-MM-dd
  startTime: string; // HH:mm
  camera: CameraType;
  season?: Season;
  altitudeM?: number;
  areaKm2?: number;
  groundCover?: GroundCover;
  notes?: string;
}

export interface SurveySummary {
  id: string;
  name: string;
  siteName: string;
  surveyDate: string;
  startTime: string;
  camera: CameraType;
  season: Season;
  timeWindow?: TimeWindow;
  status: SurveyStatus;
  imageCount: number;
  createdAt: string;
}

/** One page of a server-side list (AerialWatch.Contracts.Common.PagedResult). */
export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export type SurveySortField = 'name' | 'siteName' | 'surveyDate' | 'status' | 'imageCount' | 'createdAt';

/** GET surveys/list query (all optional). */
export interface SurveyListQuery {
  search?: string;
  date?: string; // yyyy-MM-dd
  camera?: CameraType;
  status?: SurveyStatus;
  page?: number;
  pageSize?: number;
  sort?: SurveySortField;
  dir?: 'asc' | 'desc';
}

export interface SurveyStats {
  surveys: number;
  awaitingReview: number;
  photos: number;
}

export interface SurveyDetail extends SurveySummary {
  altitudeM?: number;
  areaKm2?: number;
  groundCover: GroundCover;
  notes?: string;
  detectionProbability?: number;
  assumptions: Assumption[];
  /** AI training set of this flight; missing = not used (D-038). */
  trainingSplit?: TrainingSplit;
}
