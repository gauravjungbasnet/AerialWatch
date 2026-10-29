import { useMutation, useQuery } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import { queryKeys } from '../../api/queryKeys';
import type { CameraType, EstimatePreviewRequest } from '../../api/types';
import type { SeasonChoice } from './constants';

/** Chance a pig is visible per time window, for the chosen camera and season (or date). */
export function usePlannerWindows(camera: CameraType, seasonChoice: SeasonChoice, date: string) {
  return useQuery({
    queryKey: queryKeys.planner(camera, seasonChoice, date),
    queryFn: () => wfpApi.plannerWindows(camera, seasonChoice === 'auto' ? { date } : { season: seasonChoice }),
    enabled: Boolean(date),
    placeholderData: (previous) => previous,
  });
}

/** Corrected population estimate from a confirmed count. */
export function useEstimatePreview() {
  return useMutation({ mutationFn: (body: EstimatePreviewRequest) => wfpApi.estimatePreview(body) });
}

/** Optional environment model (temperature + ground cover). */
export function useEnvironmentProbability(camera: CameraType, canopy: number, temp: string, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.environment(camera, canopy, temp),
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
