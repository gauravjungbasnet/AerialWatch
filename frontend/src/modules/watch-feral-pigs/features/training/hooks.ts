import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import type { CameraType } from '../../api/types';
import type { TrainingSplit } from '@shared/types/training';
import { surveyKeys } from '../surveys/hooks';

export const trainingKeys = {
  all: ['wfp', 'training'] as const,
  overview: (camera: CameraType) => ['wfp', 'training', camera] as const,
};

export function useTrainingOverview(camera: CameraType) {
  return useQuery({ queryKey: trainingKeys.overview(camera), queryFn: () => wfpApi.trainingOverview(camera) });
}

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

export function useExportTrainingData() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (camera: CameraType) => wfpApi.exportTrainingData(camera),
    onSuccess: () => qc.invalidateQueries({ queryKey: trainingKeys.all }),
  });
}
