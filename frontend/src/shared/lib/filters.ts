// Filter values shared by FilterBar (what the user typed/picked) and whoever loads the data:
// a page can filter rows in the browser with applyFilters, or send the values to the API as query parameters.

/** Field key → value. Empty string means "no filter". Dates are yyyy-MM-dd (native date input). */
export type FilterValues = Record<string, string>;

export const emptyFilters = (keys: string[]): FilterValues => Object.fromEntries(keys.map((k) => [k, '']));

export const hasActiveFilters = (values: FilterValues) => Object.values(values).some((v) => v.trim() !== '');

/** Case-insensitive "contains" for free-text filters. */
export const matchesText = (haystack: string | null | undefined, needle: string) =>
  (haystack ?? '').toLocaleLowerCase().includes(needle.trim().toLocaleLowerCase());

/** For each filter key, how a row matches a (non-empty) value. */
export type FilterPredicates<T> = Record<string, (row: T, value: string) => boolean>;

/** Rows that match every active filter. Keys without a predicate are ignored (e.g. server-only filters). */
export function applyFilters<T>(rows: T[], values: FilterValues, predicates: FilterPredicates<T>): T[] {
  const active = Object.entries(values).filter(([key, value]) => value.trim() !== '' && predicates[key]);
  if (active.length === 0) return rows;
  return rows.filter((row) => active.every(([key, value]) => predicates[key](row, value)));
}

/** Only the active filters, ready for URLSearchParams when the server does the filtering. */
export const toQuery = (values: FilterValues) =>
  new URLSearchParams(
    Object.entries(values)
      .filter(([, v]) => v.trim() !== '')
      .map(([k, v]) => [k, v.trim()]),
  );
