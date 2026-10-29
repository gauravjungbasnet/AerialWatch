import { Component, lazy, Suspense, type ComponentType, type ReactElement, type ReactNode } from 'react';
import { Alert, Button, SkeletonRows } from '@shared/controls';

/**
 * A route element whose page code downloads only when the route is opened (docs/18 step 5), so e.g. the planner never
 * loads Leaflet or the review editor. Shows a skeleton while loading and a retry message if the download fails.
 *
 *   { path: 'surveys/:id/review', element: lazyPage(() => import('./features/review/ReviewPage'), 'ReviewPage') }
 */
export function lazyPage<K extends string, M extends Record<K, ComponentType>>(
  load: () => Promise<M>,
  name: K,
): ReactElement {
  const Page = lazy(async (): Promise<{ default: ComponentType }> => ({ default: (await load())[name] }));
  return (
    <PageLoadBoundary>
      <Suspense
        fallback={
          <div className="my-6" aria-busy="true">
            <SkeletonRows rows={8} />
          </div>
        }
      >
        <Page />
      </Suspense>
    </PageLoadBoundary>
  );
}

/** Catches a failed page download (offline, or a new version was deployed) instead of a blank screen. */
class PageLoadBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  state = { failed: false };
  static getDerivedStateFromError() {
    return { failed: true };
  }
  render() {
    if (!this.state.failed) return this.props.children;
    return (
      <div className="my-6">
        <Alert tone="danger" title="This page couldn't be loaded">
          Check your connection, then try again.
          <Button className="mt-3" size="sm" variant="outline" onClick={() => window.location.reload()}>
            Reload
          </Button>
        </Alert>
      </div>
    );
  }
}
