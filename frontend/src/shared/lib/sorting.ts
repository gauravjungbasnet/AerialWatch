export type SortDirection = 'asc' | 'desc';

/** Which column the grid is sorted by. null = the order the rows came in. */
export type SortState = { field: string; direction: SortDirection } | null;

/** Reads a property by name, including nested paths such as "image.takenAt". */
export function getValue(row: unknown, field: string): unknown {
  return field
    .split('.')
    .reduce<unknown>((value, part) => (value == null ? undefined : (value as Record<string, unknown>)[part]), row);
}

/** Header click cycle: not sorted → ascending → descending → not sorted. */
export function nextSort(current: SortState, field: string): SortState {
  if (current?.field !== field) return { field, direction: 'asc' };
  return current.direction === 'asc' ? { field, direction: 'desc' } : null;
}

const collator = new Intl.Collator(undefined, { numeric: true, sensitivity: 'base' });

/** Compares numbers, dates (ISO strings sort correctly as text) and text ("DJI_2" before "DJI_10"); empty values last. */
export function compareValues(a: unknown, b: unknown): number {
  const aEmpty = a == null || a === '';
  const bEmpty = b == null || b === '';
  if (aEmpty || bEmpty) return aEmpty === bEmpty ? 0 : aEmpty ? 1 : -1;
  if (typeof a === 'number' && typeof b === 'number') return a - b;
  if (typeof a === 'boolean' && typeof b === 'boolean') return Number(a) - Number(b);
  return collator.compare(String(a), String(b));
}

/** A sorted copy (stable: equal values keep their order). Empty values stay last in both directions. */
export function sortRows<T>(rows: T[], sort: SortState, valueOf: (row: T) => unknown): T[] {
  if (!sort) return rows;
  const sign = sort.direction === 'asc' ? 1 : -1;
  return rows
    .map((row, index) => ({ row, index, value: valueOf(row) }))
    .sort((x, y) => {
      const xEmpty = x.value == null || x.value === '';
      const yEmpty = y.value == null || y.value === '';
      if (xEmpty !== yEmpty) return xEmpty ? 1 : -1;
      return sign * compareValues(x.value, y.value) || x.index - y.index;
    })
    .map((x) => x.row);
}
