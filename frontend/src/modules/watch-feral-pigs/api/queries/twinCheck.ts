import { useQuery } from '@tanstack/react-query';
import { wfpApi } from '../client';
import { twinKeys } from '../keys';

/** Survey totals for the thermal/colour twin check (Results › Information, D-040). */
export function useTwinCheckSummary(surveyId: string) {
  return useQuery({
    queryKey: twinKeys.summary(surveyId),
    queryFn: () => wfpApi.twinCheckSummary(surveyId),
    enabled: Boolean(surveyId),
  });
}
