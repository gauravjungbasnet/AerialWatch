import type { AppModule } from '@shared/types/AppModule';
import { WFP_HOME, wfpPath } from './paths';
import { PiggyBank } from 'lucide-react';
import { feralPigsNavigation } from './navigation';
import { feralPigsRoutes } from './routes';

/** Public module definition. Feature implementation stays inside this folder. */
export const watchFeralPigs = {
  code: 'WFP',
  name: 'Watch Feral Pigs',
  icon: PiggyBank,
  home: WFP_HOME,
  available: true,
  navigation: feralPigsNavigation,
  routes: feralPigsRoutes,
  primaryAction: { to: wfpPath('/surveys/new'), label: 'New survey' },
} satisfies AppModule;
