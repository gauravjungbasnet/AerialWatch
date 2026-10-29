import type { AppRoute } from '@shared/types/routes';
import { DashboardPage } from './features/dashboard/DashboardPage';
import { PlannerPage } from './features/planner/PlannerPage';
import { AboutPage } from './features/about/AboutPage';
import { SurveysPage } from './features/surveys/SurveysPage';
import { NewSurveyPage } from './features/surveys/NewSurveyPage';
import { SurveyDetailPage } from './features/surveys/SurveyDetailPage';
import { ModelParametersPage } from './features/admin/ModelParametersPage';
import { ReviewPage } from './features/review/ReviewPage';
import { TrainingDataPage } from './features/training/TrainingDataPage';
import { ResultsPage } from './features/results/ResultsPage';

export const feralPigsRoutes: AppRoute[] = [
  { index: true, element: <DashboardPage /> },
  { path: 'planner', element: <PlannerPage /> },
  { path: 'surveys', element: <SurveysPage /> },
  { path: 'surveys/new', element: <NewSurveyPage /> },
  { path: 'surveys/:id', element: <SurveyDetailPage /> },
  { path: 'surveys/:id/review', element: <ReviewPage /> },
  { path: 'surveys/:id/results', element: <ResultsPage />, handle: { fullBleed: true } },
  { path: 'about', element: <AboutPage /> },
  { path: 'admin/model-parameters', element: <ModelParametersPage /> },
  { path: 'admin/training-data', element: <TrainingDataPage /> },
];
