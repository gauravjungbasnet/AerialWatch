// Ranger review: queue, photo detail, boxes (D-034). Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { CameraType } from './common';
import type { SurveyStatus } from './surveys';

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
