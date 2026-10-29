import type { AppRoute } from '@shared/types/routes';
import { lazyPage } from '@shared/routing/lazyPage';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { PlannerPage } from './features/planner/PlannerPage';
import { AboutPage } from './features/about/AboutPage';
import { SurveysPage } from './features/surveys/SurveysPage';
import { NewSurveyPage } from './features/surveys/NewSurveyPage';
import { SurveyDetailPage } from './features/surveys/SurveyDetailPage';

// Everyday pages load with the app; the review editor, the results map (Leaflet) and the admin pages load on demand.
export const feralPigsRoutes: AppRoute[] = [
  { index: true, element: <DashboardPage /> },
  { path: 'planner', element: <PlannerPage /> },
  { path: 'surveys', element: <SurveysPage /> },
  { path: 'surveys/new', element: <NewSurveyPage /> },
  { path: 'surveys/:id', element: <SurveyDetailPage /> },
  { path: 'surveys/:id/review', element: lazyPage(() => import('./features/review/ReviewPage'), 'ReviewPage') },
  {
    path: 'surveys/:id/results',
    element: lazyPage(() => import('./features/results/ResultsPage'), 'ResultsPage'),
    handle: { fullBleed: true },
  },
  { path: 'about', element: <AboutPage /> },
  {
    path: 'admin/model-parameters',
    element: lazyPage(() => import('./features/admin/ModelParametersPage'), 'ModelParametersPage'),
  },
  {
    path: 'admin/training-data',
    element: lazyPage(() => import('./features/training/TrainingDataPage'), 'TrainingDataPage'),
  },
];
