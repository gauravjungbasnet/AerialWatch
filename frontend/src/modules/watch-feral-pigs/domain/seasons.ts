import type { Season } from '../api/types';

/** Planner season picker: a season, or 'auto' to take it from the survey date. */
export type SeasonChoice = 'auto' | Season;
