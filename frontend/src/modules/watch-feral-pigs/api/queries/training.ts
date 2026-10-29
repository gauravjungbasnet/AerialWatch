import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { TrainingSplit } from '@shared/types/training';
import { wfpApi } from '../client';
import { surveyKeys, trainingKeys } from '../keys';

/** Survey page card: put the survey in a set (or null = not used); refreshes the survey and the training page. */
export function useSetTrainingSplit(surveyId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (split: TrainingSplit | null) => wfpApi.setTrainingSplit(surveyId, split),
    onSuccess: () =>
      Promise.all([
        qc.invalidateQueries({ queryKey: surveyKeys.detail(surveyId), exact: true }),
        qc.invalidateQueries({ queryKey: trainingKeys.all }),
      ]),
  });
}
