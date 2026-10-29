import type { IndexRouteObject, NonIndexRouteObject } from 'react-router-dom';

/** Module-owned layout hints consumed by the platform shell. */
export interface RouteLayout {
  fullBleed?: boolean;
}

export type AppRoute =
  | (Omit<IndexRouteObject, 'handle'> & { handle?: RouteLayout })
  | (Omit<NonIndexRouteObject, 'handle' | 'children'> & { handle?: RouteLayout; children?: AppRoute[] });
