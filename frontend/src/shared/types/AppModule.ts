import type { LucideIcon } from 'lucide-react';
import type { AppRoute } from './routes';
import type { NavSection } from '../components/navigation/types';

export interface AppModule {
  code: string;
  name: string;
  icon: LucideIcon;
  home: string;
  available: boolean;
  navigation: NavSection[];
  routes: AppRoute[];
  primaryAction?: { to: string; label: string };
}
