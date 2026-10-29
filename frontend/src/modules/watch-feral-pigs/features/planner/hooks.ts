import { useMutation, useQuery } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import { modelKeys } from '../../api/keys';
import type { CameraType, EstimatePreviewRequest } from '../../api/types';

/** Corrected population estimate from a confirmed count. */
export function useEstimatePreview() {
  return useMutation({ mutationFn: (body: EstimatePreviewRequest) => wfpApi.estimatePreview(body) });
}

/** Optional environment model (temperature + ground cover). */
export function useEnvironmentProbability(camera: CameraType, canopy: number, temp: string, enabled: boolean) {
  return useQuery({
    queryKey: modelKeys.environment(camera, canopy, temp),
    queryFn: () =>
      wfpApi.probability({
        camera,
        model: 'environment',
        canopyProportion: canopy,
        ambientTemperatureC: camera === 'thermal' ? Number(temp) : undefined,
      }),
    enabled: enabled && (camera === 'rgb' || temp !== ''),
  });
}
