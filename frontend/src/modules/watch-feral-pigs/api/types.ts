import type { TrainingSplit, TrainingSplitTotals } from '@shared/types/training';
// Mirrors the .NET API contracts (backend/AerialWatch.Contracts). Enums are camelCase strings.

export type Season = 'wet' | 'dry';
export type CameraType = 'thermal' | 'rgb';
export type TimeWindow = 'earlyMorning' | 'midMorning' | 'midAfternoon' | 'earlyEvening';
export type DetectionModelKind = 'timeSeason' | 'environment';

export interface Assumption {
  level: 'info' | 'warning';
  message: string;
}

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

// ---- Surveys ----
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

// ---- Survey photos (D-030) ----
export interface SurveyImage {
  id: string;
  surveyId: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  widthPx?: number;
  heightPx?: number;
  takenAt?: string;
  latitude?: number;
  longitude?: number;
  hasThumbnail: boolean;
  reviewed: boolean;
  uploadedAt: string;
  /** Camera that took this photo (D-039). */
  camera: CameraType;
  /** True for the other camera's photo: shown next to its twin, never counted. */
  isReference: boolean;
  /** The other camera's photo of the same moment, when uploaded. */
  twinId?: string;
  /** GPS was copied from the twin because this photo had none. */
  locationFromTwin: boolean;
}

export type UploadFileStatus = 'uploaded' | 'duplicate' | 'rejected';

export interface UploadFileResult {
  fileName: string;
  status: UploadFileStatus;
  message?: string;
  image?: SurveyImage;
}

export interface UploadImagesResponse {
  surveyId: string;
  uploaded: number;
  duplicates: number;
  rejected: number;
  totalImages: number;
  files: UploadFileResult[];
}

// ---- Review (D-034) ----
export type DetectionSource = 'ai' | 'ranger';
export type DetectionStatus = 'pending' | 'accepted' | 'rejected';
export type ReviewImageFilter = 'all' | 'notReviewed' | 'pending' | 'withPigs' | 'noPigs' | 'flagged';

/** A photo in the review queue with its box counts (ReviewImageDto). */
export interface ReviewImage {
  id: string;
  fileName: string;
  widthPx?: number;
  heightPx?: number;
  takenAt?: string;
  latitude?: number;
  longitude?: number;
  hasThumbnail: boolean;
  reviewed: boolean;
  flagged: boolean;
  reviewedBy?: string;
  reviewedAt?: string;
  /** Confirmed pigs: accepted AI boxes + boxes drawn by a ranger. */
  confirmed: number;
  /** AI suggestions not yet accepted or rejected. */
  pending: number;
  rejected: number;
}

/** One box in original-image pixels, top-left origin (DetectionDto). */
export interface Detection {
  id: string;
  imageId: string;
  x: number;
  y: number;
  width: number;
  height: number;
  label: string;
  confidence?: number;
  source: DetectionSource;
  status: DetectionStatus;
  modelVersion?: string;
  reviewedBy?: string;
  reviewedAt?: string;
}

/** The other camera's photo of the same moment, shown beside the photo under review (D-039). */
export interface TwinImage {
  id: string;
  fileName: string;
  camera: CameraType;
  widthPx?: number;
  heightPx?: number;
  takenAt?: string;
  hasThumbnail: boolean;
}

export interface ReviewImageDetail {
  image: ReviewImage;
  detections: Detection[];
  twin?: TwinImage;
}

export interface ReviewSummary {
  surveyId: string;
  status: SurveyStatus;
  photos: number;
  reviewedPhotos: number;
  flaggedPhotos: number;
  photosWithPigs: number;
  /** The confirmed count C used by the estimate. */
  confirmedPigs: number;
  pendingSuggestions: number;
  rejectedSuggestions: number;
  aiSuggestions: number;
  rangerAdded: number;
  canComplete: boolean;
  blockedReason?: string;
}

export interface BoxRect {
  x: number;
  y: number;
  width: number;
  height: number;
}

export interface UpdateDetectionRequest extends Partial<BoxRect> {
  status?: DetectionStatus;
}

export interface BulkAcceptResponse {
  accepted: number;
  summary: ReviewSummary;
}

// ---- Admin: model parameters ----
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

// ---- AI training data (D-038) ----
export interface TrainingSurvey {
  id: string;
  name: string;
  surveyDate: string;
  startTime: string;
  status: SurveyStatus;
  split: TrainingSplit;
  photos: number;
  checkedPhotos: number;
  flaggedPhotos: number;
  usablePhotos: number;
  photosWithPigs: number;
  pigs: number;
}

export interface TrainingOverview {
  camera: CameraType;
  exportRoot: string;
  surveys: TrainingSurvey[];
  totals: TrainingSplitTotals[];
  warnings: string[];
  canExport: boolean;
}

export interface SurveyTrainingSplit {
  surveyId: string;
  split?: TrainingSplit;
}

// ---- Results (Slice 5) ----
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

// ---- Twin check (D-040) ----
/** suggested = the app's guess; confirmed = placed or accepted by a ranger; notVisible = can't be seen on the twin. */
export type TwinMarkState = 'suggested' | 'confirmed' | 'notVisible';

export interface TwinMark {
  detectionId: string;
  /** Same number as the box label on the counted photo. */
  number: number;
  state: TwinMarkState;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

/** A pig drawn first on the twin photo (D-041): suggested on the counted photo until confirmed or marked not visible. */
export interface TwinSighting {
  markId: string;
  /** "c1", "c2"… on a colour twin ("t1"… on a thermal twin). */
  label: string;
  state: 'suggestedOnCounted' | 'notVisibleOnCounted';
  twinX: number;
  twinY: number;
  twinWidth: number;
  twinHeight: number;
  /** Suggested position on the counted photo. */
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

export interface TwinCheck {
  imageId: string;
  twinImageId?: string;
  /** Ranger-placed pairs in this survey the suggestions are fitted to. */
  learnedFrom: number;
  marks: TwinMark[];
  sightings: TwinSighting[];
}

export interface SetTwinMarkRequest {
  state: Exclude<TwinMarkState, 'suggested'>;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

export interface TwinCheckSummary {
  twinCamera?: CameraType;
  pigsWithTwin: number;
  checked: number;
  visibleOnTwin: number;
  notVisibleOnTwin: number;
  /** Drawn on the other camera's photo and not visible on the counted one (never counted). */
  seenOnlyOnTwin: number;
}
