import type { AppModule } from '@shared/types/AppModule';
import { watchFeralPigs } from '@modules/watch-feral-pigs';
import { watchWetlands } from '@modules/watch-wetlands';
import { watchGhostNet } from '@modules/watch-ghost-net';

export const defaultApp: AppModule = watchFeralPigs;
export const apps: readonly AppModule[] = [watchFeralPigs, watchWetlands, watchGhostNet];

/** Match a whole URL segment, so /wfp-other never selects WFP. */
export function getAppForPath(pathname: string): AppModule | undefined {
  return apps.find((app) => pathname === app.home || pathname.startsWith(app.home + '/'));
}
