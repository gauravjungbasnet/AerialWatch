import { useQuery } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import { resultsKeys } from '../../api/keys';

/** Results for one survey. Refetched every time the page opens, so review changes show straight away. */
export function useSurveyResults(surveyId: string) {
  return useQuery({
    queryKey: resultsKeys.survey(surveyId),
    queryFn: () => wfpApi.surveyResults(surveyId),
    enabled: Boolean(surveyId),
    refetchOnMount: 'always',
  });
}

/** Read-only evidence for one photo on the map: its boxes, its twin and the twin check (D-039–D-041). */
export function usePhotoEvidence(surveyId: string, imageId: string | undefined) {
  const detail = useQuery({
    queryKey: resultsKeys.photo(surveyId, imageId ?? ''),
    queryFn: () => wfpApi.reviewImage(imageId!),
    enabled: Boolean(imageId),
    refetchOnMount: 'always',
  });
  const hasTwin = Boolean(detail.data?.twin);
  const twin = useQuery({
    queryKey: resultsKeys.photoTwin(surveyId, imageId ?? ''),
    queryFn: () => wfpApi.twinCheck(imageId!),
    enabled: Boolean(imageId) && hasTwin,
    refetchOnMount: 'always',
  });
  return { detail, twin };
}
