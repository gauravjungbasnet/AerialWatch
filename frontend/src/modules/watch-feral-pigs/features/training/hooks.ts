import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import type { CameraType } from '../../api/types';
import { trainingKeys } from '../../api/keys';

export function useTrainingOverview(camera: CameraType) {
  return useQuery({ queryKey: trainingKeys.overview(camera), queryFn: () => wfpApi.trainingOverview(camera) });
}

export function useExportTrainingData() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (camera: CameraType) => wfpApi.exportTrainingData(camera),
    onSuccess: () => qc.invalidateQueries({ queryKey: trainingKeys.all }),
  });
}
