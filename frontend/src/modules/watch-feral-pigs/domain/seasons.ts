import type { Season } from '../api/types';

/** Planner season picker: a season, or 'auto' to take it from the survey date. */
export type SeasonChoice = 'auto' | Season;

/** NT convention used by the API: wet Nov–Apr, dry May–Oct. */
export function seasonFromDate(iso: string): Season {
  const month = Number(iso.slice(5, 7));
  return month >= 5 && month <= 10 ? 'dry' : 'wet';
}
