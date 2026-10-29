import { Waves, Home } from 'lucide-react';
import type { AppModule } from '@shared/types/AppModule';
import { ComingSoonPage } from '@shared/pages/ComingSoonPage';

const name = 'Watch Wetlands';
const home = '/wwl';

export const watchWetlands: AppModule = {
  code: 'WWL',
  name,
  icon: Waves,
  home,
  available: false,
  navigation: [{ items: [{ to: home, label: 'Overview', icon: <Home className="size-5" aria-hidden="true" /> }] }],
  routes: [{ index: true, element: <ComingSoonPage name={name} /> }],
};
