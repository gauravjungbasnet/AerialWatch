// Central TanStack Query keys, so caches can be invalidated consistently as features grow.
export const queryKeys = {
  planner: (camera: string, season: string, date: string) => ['wfp', 'planner', camera, season, date] as const,
  environment: (camera: string, canopy: number, temp: string) => ['wfp', 'environment', camera, canopy, temp] as const,
  modelInfo: ['wfp', 'model-info'] as const,
};
