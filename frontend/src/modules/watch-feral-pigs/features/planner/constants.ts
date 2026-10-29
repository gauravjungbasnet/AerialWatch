import type { Season } from '../../api/types';
import type { SelectOption } from '@shared/controls';

import type { SeasonChoice } from '../../domain/seasons';
export type { SeasonChoice };

export { CAMERA_OPTIONS } from '../../domain/cameras';

export const seasonOptions = (autoSeason: Season): SelectOption<SeasonChoice>[] => [
  { value: 'auto', label: `Auto (${autoSeason})` },
  { value: 'wet', label: 'Wet (Nov–Apr)' },
  { value: 'dry', label: 'Dry (May–Oct)' },
];

/** Ground cover → approximate proportion of time under canopy (our assumption; see docs/00 D-005). */
export const GROUND_COVER = {
  open: { label: 'Open (few trees)', canopy: 0.05 },
  mixed: { label: 'Mixed (some trees)', canopy: 0.4 },
  dense: { label: 'Dense (mostly canopy)', canopy: 0.8 },
} as const;
export type GroundCoverKey = keyof typeof GROUND_COVER;

export const GROUND_COVER_OPTIONS: SelectOption<GroundCoverKey>[] = (Object.keys(GROUND_COVER) as GroundCoverKey[]).map(
  (k) => ({
    value: k,
    label: GROUND_COVER[k].label,
  }),
);
