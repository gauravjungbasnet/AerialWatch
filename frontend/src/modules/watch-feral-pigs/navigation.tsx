import { wfpPath } from './paths';
import type { NavSection } from '@shared/components/navigation/types';
import { CalendarClock, Database, Home, Info, ListChecks, SlidersHorizontal } from 'lucide-react';

const icon = (C: typeof Home) => <C className="size-5" aria-hidden="true" />;

/** Single source of truth for this module's sidebar and header navigation. */
export const feralPigsNavigation: NavSection[] = [
  {
    items: [
      { to: wfpPath(''), label: 'Dashboard', icon: icon(Home) },
      { to: wfpPath('/planner'), label: 'Survey planner', icon: icon(CalendarClock) },
      {
        to: wfpPath('/surveys'),
        label: 'Surveys',
        icon: icon(ListChecks),
        isActive: (p) => p.startsWith(wfpPath('/surveys')),
      },
      { to: wfpPath('/about'), label: 'About the research', icon: icon(Info), placement: 'header' },
    ],
  },
  {
    title: 'Admin',
    items: [
      { to: wfpPath('/admin/model-parameters'), label: 'Model parameters', icon: icon(SlidersHorizontal) },
      { to: wfpPath('/admin/training-data'), label: 'AI training data', icon: icon(Database) },
    ],
  },
];
