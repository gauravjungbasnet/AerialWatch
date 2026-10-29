// Results and photo map (Slice 5). Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { Assumption } from './common';
import type { EstimateResponse } from './planner';
import type { SurveyStatus } from './surveys';

export interface ResultCounts {
  photos: number;
  reviewedPhotos: number;
  flaggedPhotos: number;
  photosWithPigs: number;
  photosWithGps: number;
  /** Accepted boxes, AI-suggested or ranger-drawn. */
  confirmed: number;
  confirmedFromAi: number;
  drawnByRanger: number;
  rejectedAi: number;
  pendingAi: number;
}

export interface PhotoPoint {
  imageId: string;
  fileName: string;
  latitude?: number;
  longitude?: number;
  takenAt?: string;
  confirmed: number;
  reviewed: boolean;
  flagged: boolean;
}

export interface SurveyResults {
  surveyId: string;
  status: SurveyStatus;
  provisional: boolean;
  provisionalReason?: string;
  counts: ResultCounts;
  /** Missing when the research model can't be applied (e.g. a night flight). */
  estimate?: EstimateResponse;
  estimateUnavailableReason?: string;
  photos: PhotoPoint[];
  notes: Assumption[];
}
