/** Number of pages for a row count (at least 1, so an empty grid still shows "page 1 of 1"). */
export const pageCount = (total: number, pageSize: number) => Math.max(1, Math.ceil(total / pageSize));

/**
 * A sliding window of page numbers around the current page (first/last buttons handle the jumps).
 * pageWindow(1, 7) → [1, 2, 3, 4]; pageWindow(5, 7) → [4, 5, 6, 7]; pageWindow(2, 3) → [1, 2, 3].
 */
export function pageWindow(current: number, total: number, size = 4): number[] {
  const count = Math.min(size, total);
  let start = Math.max(1, current - Math.floor((count - 1) / 2));
  start = Math.min(start, total - count + 1);
  return Array.from({ length: count }, (_, i) => start + i);
}
