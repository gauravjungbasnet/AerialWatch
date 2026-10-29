import type {
  BoxRect,
  BulkAcceptResponse,
  Detection,
  ReviewImage,
  ReviewImageDetail,
  ReviewImageFilter,
  ReviewSummary,
  UpdateDetectionRequest,
} from '../types';
import { client } from '../http';

/** WFP review endpoints (/api/wfp/...). */
export const reviewApi = {
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
};
