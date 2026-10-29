import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import type { CreateSurveyRequest, SurveyListQuery } from '../../api/types';

export const surveyKeys = {
  all: ['wfp', 'surveys'] as const,
  lists: ['wfp', 'surveys', 'list'] as const,
  list: (query: SurveyListQuery) => ['wfp', 'surveys', 'list', query] as const,
  stats: ['wfp', 'surveys', 'stats'] as const,
  detail: (id: string) => ['wfp', 'surveys', id] as const,
  images: (id: string) => ['wfp', 'surveys', id, 'images'] as const,
};
const keys = surveyKeys;

/** One page from the server. Keeps showing the previous page while the next one loads (no flicker). */
export function useSurveys(query: SurveyListQuery) {
  return useQuery({
    queryKey: keys.list(query),
    queryFn: () => wfpApi.listSurveys(query),
    placeholderData: keepPreviousData,
  });
}

export function useSurveyStats() {
  return useQuery({ queryKey: keys.stats, queryFn: wfpApi.surveyStats });
}

export function useSurvey(id: string) {
  return useQuery({ queryKey: keys.detail(id), queryFn: () => wfpApi.getSurvey(id), enabled: Boolean(id) });
}

export function useCreateSurvey() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (body: CreateSurveyRequest) => wfpApi.createSurvey(body),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.all }),
  });
}

export function useDeleteSurvey() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => wfpApi.deleteSurvey(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: keys.all }),
  });
}

export function useSurveyImages(surveyId: string) {
  return useQuery({
    queryKey: keys.images(surveyId),
    queryFn: () => wfpApi.listSurveyImages(surveyId),
    enabled: Boolean(surveyId),
  });
}

/** Refreshes everything that changes when photos are added or removed (photo list, count, status). */
export function useRefreshSurveyPhotos(surveyId: string) {
  const qc = useQueryClient();
  return () =>
    Promise.all([
      qc.invalidateQueries({ queryKey: keys.images(surveyId) }),
      qc.invalidateQueries({ queryKey: keys.detail(surveyId), exact: true }),
      qc.invalidateQueries({ queryKey: keys.lists }),
      qc.invalidateQueries({ queryKey: keys.stats }),
    ]);
}

export function useDeleteSurveyImage(surveyId: string) {
  const refresh = useRefreshSurveyPhotos(surveyId);
  return useMutation({ mutationFn: (imageId: string) => wfpApi.deleteSurveyImage(imageId), onSuccess: refresh });
}
