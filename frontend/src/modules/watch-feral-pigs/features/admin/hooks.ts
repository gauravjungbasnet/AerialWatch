import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import type { CoefficientEntry } from '../../api/types';

const key = ['wfp', 'admin', 'model-parameters'] as const;

export function useModelParameters() {
  return useQuery({ queryKey: key, queryFn: wfpApi.getModelParameters });
}

/** Saved versions, newest first (one per save or reset). */
export function useModelParametersHistory(take = 10) {
  return useQuery({ queryKey: [...key, 'history', take], queryFn: () => wfpApi.modelParametersHistory(take) });
}

/** After any change, refetch everything so planner, estimates and surveys use the new coefficients. */
function useInvalidateAll() {
  const qc = useQueryClient();
  return () => qc.invalidateQueries({ queryKey: ['wfp'] });
}

export function useSaveModelParameters() {
  const invalidate = useInvalidateAll();
  return useMutation({
    mutationFn: ({ values, updatedBy }: { values: CoefficientEntry[]; updatedBy?: string }) =>
      wfpApi.updateModelParameters(values, updatedBy),
    onSuccess: invalidate,
  });
}

export function useResetModelParameters() {
  const invalidate = useInvalidateAll();
  return useMutation({ mutationFn: wfpApi.resetModelParameters, onSuccess: invalidate });
}
