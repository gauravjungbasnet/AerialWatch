// Survey photos and uploads (D-030). Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { CameraType } from './common';

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
