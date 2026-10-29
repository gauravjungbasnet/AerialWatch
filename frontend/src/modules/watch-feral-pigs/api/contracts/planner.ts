// Survey planner, chance-visible model and estimate preview. Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { Assumption, CameraType, DetectionModelKind, Season, TimeWindow } from './common';

export interface PlannerWindow {
  window: TimeWindow;
  label: string;
  studyHours: string;
  probability: number;
  isBest: boolean;
  isWorst: boolean;
  note?: string;
}

export interface PlannerResult {
  season: Season;
  camera: CameraType;
  windows: PlannerWindow[];
  recommendation: string;
  modelVersion: string;
  assumptions: Assumption[];
}

export interface ProbabilityRequest {
  camera: CameraType;
  window?: TimeWindow;
  time?: string;
  season?: Season;
  date?: string;
  model?: DetectionModelKind;
  ambientTemperatureC?: number;
  canopyProportion?: number;
}

export interface ProbabilityResponse {
  probability: number;
  model: DetectionModelKind;
  modelVersion: string;
  window?: TimeWindow;
  season?: Season;
  assumptions: Assumption[];
}

export interface EstimatePreviewRequest {
  confirmedCount: number;
  camera: CameraType;
  surveyDate: string;
  startTime: string;
  season?: Season;
  areaKm2?: number;
  model?: DetectionModelKind;
  ambientTemperatureC?: number;
  canopyProportion?: number;
}

export interface EstimateResponse {
  confirmedCount: number;
  detectionProbability: number;
  estimate: number;
  estimateRounded: number;
  low: number;
  high: number;
  densityPerKm2?: number;
  method: string;
  model: DetectionModelKind;
  modelVersion: string;
  window?: TimeWindow;
  season: Season;
  provisional: boolean;
  assumptions: Assumption[];
}

export interface ModelInfo {
  modelVersion: string;
  citation: string;
  doi: string;
  studyConditions: string[];
  ourAssumptions: string[];
}

/** RFC 7807 validation problem returned by the API on 400. */
