import { Navigate, useRoutes } from 'react-router-dom';
import { AppLayout } from './layout/AppLayout';
import { apps, defaultApp } from './registry';
import { NotFoundPage } from '@shared/pages/NotFoundPage';

export default function App() {
  return useRoutes([
    { path: '/', element: <Navigate to={defaultApp.home} replace /> },
    ...apps.map((app) => ({
      path: app.home,
      element: <AppLayout />,
      children: [...app.routes, { path: '*', element: <NotFoundPage home={app.home} /> }],
    })),
    { path: '*', element: <NotFoundPage home={defaultApp.home} /> },
  ]);
}
