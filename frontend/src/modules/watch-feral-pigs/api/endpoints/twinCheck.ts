import type { BoxRect, SetTwinMarkRequest, TwinCheck, TwinCheckSummary, TwinMark, TwinSighting } from '../types';
import { client } from '../http';

/** WFP twinCheck endpoints (/api/wfp/...). */
export const twinCheckApi = {
  twinCheck(imageId: string) {
    return client.request<TwinCheck>(`/twinCheck/get/${encodeURIComponent(imageId)}`);
  },

  setTwinMark(detectionId: string, body: SetTwinMarkRequest) {
    return client.request<TwinMark>(`/twinCheck/set/${encodeURIComponent(detectionId)}`, {
      method: 'PUT',
      body: JSON.stringify(body),
    });
  },

  clearTwinMark(detectionId: string) {
    return client.request<void>(`/twinCheck/clear/${encodeURIComponent(detectionId)}`, { method: 'DELETE' });
  },

  addTwinSighting(imageId: string, box: BoxRect) {
    return client.request<TwinSighting>(`/twinCheck/addSighting/${encodeURIComponent(imageId)}`, {
      method: 'POST',
      body: JSON.stringify(box),
    });
  },

  confirmTwinSighting(markId: string, box: BoxRect) {
    return client.request<TwinMark>(`/twinCheck/confirmSighting/${encodeURIComponent(markId)}`, {
      method: 'POST',
      body: JSON.stringify(box),
    });
  },

  setTwinSightingVisibility(markId: string, notVisibleOnCounted: boolean) {
    return client.request<TwinSighting>(`/twinCheck/sightingVisibility/${encodeURIComponent(markId)}`, {
      method: 'PUT',
      body: JSON.stringify({ notVisibleOnCounted }),
    });
  },

  removeTwinSighting(markId: string) {
    return client.request<void>(`/twinCheck/removeSighting/${encodeURIComponent(markId)}`, { method: 'DELETE' });
  },

  twinCheckSummary(surveyId: string) {
    return client.request<TwinCheckSummary>(`/twinCheck/summary/${encodeURIComponent(surveyId)}`);
  },
};
