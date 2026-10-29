// Admin: model coefficients (D-013, D-028). Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { CameraType, DetectionModelKind } from './common';

export type CoefficientTerm =
  | 'intercept'
  | 'seasonDry'
  | 'midAfternoon'
  | 'midMorning'
  | 'earlyMorning'
  | 'ambientTemperatureC'
  | 'timeUnderCanopy';

/** One coefficient (a row in wfp.CoefficientTypes) with its published value and the value in use. */
export interface ModelCoefficient {
  code: string;
  camera: CameraType;
  model: DetectionModelKind;
  term: CoefficientTerm;
  label: string;
  description: string;
  sortOrder: number;
  published: number;
  current: number;
}

/** A value by coefficient code, e.g. { code: 'thermal.timeSeason.intercept', value: 0.951 }. */
export interface CoefficientEntry {
  code: string;
  value: number;
}

export interface ModelParametersResponse {
  version: string;
  isPublished: boolean;
  updatedAt: string;
  updatedBy?: string;
  coefficients: ModelCoefficient[];
  source: string;
}

export interface ModelParametersHistoryItem {
  id: string;
  version: string;
  isPublished: boolean;
  savedAt: string;
  savedBy?: string;
  values: CoefficientEntry[];
}
