import type { SurveyImage, UploadImagesResponse } from '../types';
import { client } from '../http';

/** WFP photos endpoints (/api/wfp/...). */
export const photosApi = {
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
};
