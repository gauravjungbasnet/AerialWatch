import { useQuery } from '@tanstack/react-query';
import { wfpApi } from '../client';
import { modelKeys } from '../keys';
import type { CameraType } from '../types';
import type { SeasonChoice } from '../../domain/seasons';

/** Chance a pig is visible per time window, for the chosen camera and season (or date). */
export function usePlannerWindows(camera: CameraType, seasonChoice: SeasonChoice, date: string) {
  return useQuery({
    queryKey: modelKeys.planner(camera, seasonChoice, date),
    queryFn: () => wfpApi.plannerWindows(camera, seasonChoice === 'auto' ? { date } : { season: seasonChoice }),
    enabled: Boolean(date),
    placeholderData: (previous) => previous,
  });
}
