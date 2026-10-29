import type { CoefficientEntry, ModelParametersHistoryItem, ModelParametersResponse } from '../types';
import { client } from '../http';

/** WFP admin endpoints (/api/wfp/...). */
export const adminApi = {
  getModelParameters() {
    return client.request<ModelParametersResponse>('/modelParameters/get');
  },

  updateModelParameters(values: CoefficientEntry[], updatedBy?: string) {
    return client.request<ModelParametersResponse>('/modelParameters/update', {
      method: 'PUT',
      body: JSON.stringify({ values, updatedBy }),
    });
  },

  modelParametersHistory(take = 20) {
    return client.request<ModelParametersHistoryItem[]>(`/modelParameters/history?take=${take}`);
  },

  resetModelParameters() {
    return client.request<ModelParametersResponse>('/modelParameters/reset', { method: 'POST' });
  },
};
