import { Fish, Home } from 'lucide-react';
import type { AppModule } from '@shared/types/AppModule';
import { ComingSoonPage } from '@shared/pages/ComingSoonPage';

const name = 'Watch Ghost Net';
const home = '/wgn';

export const watchGhostNet: AppModule = {
  code: 'WGN',
  name,
  icon: Fish,
  home,
  available: false,
  navigation: [{ items: [{ to: home, label: 'Overview', icon: <Home className="size-5" aria-hidden="true" /> }] }],
  routes: [{ index: true, element: <ComingSoonPage name={name} /> }],
};
