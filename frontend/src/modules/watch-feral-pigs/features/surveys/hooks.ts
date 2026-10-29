import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import { surveyKeys } from '../../api/keys';
import type { CreateSurveyRequest } from '../../api/types';

// Kept here for features/surveys/photos (moves in docs/18 step 6); other features import from api/queries/surveys.
export { surveyKeys } from '../../api/keys';
export { useSurvey, useSurveys, useSurveyStats } from '../../api/queries/surveys';
const keys = surveyKeys;

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
