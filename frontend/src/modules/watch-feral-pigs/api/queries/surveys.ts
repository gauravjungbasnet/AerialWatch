import { keepPreviousData, useQuery } from '@tanstack/react-query';
import { wfpApi } from '../client';
import { surveyKeys } from '../keys';
import type { SurveyListQuery } from '../types';

// Survey reads used by several features (dashboard, surveys, review, results, training).

/** One page from the server. Keeps showing the previous page while the next one loads (no flicker). */
export function useSurveys(query: SurveyListQuery) {
  return useQuery({
    queryKey: surveyKeys.list(query),
    queryFn: () => wfpApi.listSurveys(query),
    placeholderData: keepPreviousData,
  });
}

export function useSurveyStats() {
  return useQuery({ queryKey: surveyKeys.stats, queryFn: wfpApi.surveyStats });
}

export function useSurvey(id: string) {
  return useQuery({ queryKey: surveyKeys.detail(id), queryFn: () => wfpApi.getSurvey(id), enabled: Boolean(id) });
}
