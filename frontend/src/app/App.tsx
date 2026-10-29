import { Navigate, useRoutes } from 'react-router-dom';
import { AppLayout } from './layout/AppLayout';
import { apps, defaultApp } from './registry';
import { NotFoundPage } from '@shared/pages/NotFoundPage';
import { lazyPage } from '@shared/routing/lazyPage';

// Development only: the shared-components example (docs/18 step 7). Not part of a production build.
const devRoutes = import.meta.env.DEV
  ? [
      {
        path: '/dev/shared',
        element: lazyPage(() => import('./dev/SharedComponentsExample'), 'SharedComponentsExample'),
      },
    ]
  : [];

export default function App() {
  return useRoutes([
    { path: '/', element: <Navigate to={defaultApp.home} replace /> },
    ...devRoutes,
    ...apps.map((app) => ({
      path: app.home,
      element: <AppLayout />,
      children: [...app.routes, { path: '*', element: <NotFoundPage home={app.home} /> }],
    })),
    { path: '*', element: <NotFoundPage home={defaultApp.home} /> },
  ]);
}
