import { useMemo, useState, type ReactNode } from 'react';
import {
  ArrowDown,
  ArrowUp,
  ChevronDown,
  ChevronLeft,
  ChevronRight,
  ChevronsLeft,
  ChevronsRight,
  ChevronsUpDown,
} from 'lucide-react';
import { cx } from '../lib/format';
import { pageCount, pageWindow } from '../lib/pagination';
import { getValue, nextSort, sortRows, type SortState } from '../lib/sorting';

/**
 * One column, declared as config. Minimum: { field: 'fileName', header: 'File' }.
 * `field` is the row property shown (dot paths like 'image.takenAt' work) and is also the sort key
 * sent to the server. Add `cell` only when the value needs formatting (badges, dates, buttons).
 */
export interface DataGridColumn<T> {
  /** Property name of the value in each row; unique per grid. */
  field: string;
  /** Column title shown in the header. */
  header: ReactNode;
  /** Clicking the header sorts by this column (ascending → descending → off). */
  sortable?: boolean;
  /** Custom display. Default: the field's value as text ('—' when empty). */
  cell?: (row: T) => ReactNode;
  /** Value used for sorting in the browser when it differs from the field (e.g. a status order). */
  sortValue?: (row: T) => unknown;
  /** Extra classes for the body cells (e.g. 'whitespace-normal min-w-64' for long text). */
  className?: string;
  headerClassName?: string;
  align?: 'left' | 'center' | 'right';
  /** Keep the column visible at the right edge while the table scrolls sideways (e.g. an Actions column). */
  stickyRight?: boolean;
  /** Tooltip for the cell, e.g. the full text of a truncated value. */
  title?: (row: T) => string | undefined;
}

/**
 * Server-side paging: the API returns one page plus the total, and the page owns page/pageSize
 * (e.g. in a TanStack Query key together with the filter values).
 */
export interface ServerPaging {
  page: number;
  pageSize: number;
  total: number;
  onPageChange: (page: number) => void;
  onPageSizeChange: (pageSize: number) => void;
}

/** Server-side sorting: the page sends `sort` to the API (e.g. ?sort=fileName&dir=asc). */
export interface ServerSorting {
  sort: SortState;
  onSortChange: (sort: SortState) => void;
}

export interface DataGridProps<T> {
  columns: DataGridColumn<T>[];
  /** Client mode: all rows (the grid pages them). Server mode: just the current page. */
  rows: T[];
  rowKey: (row: T, index: number) => string;
  /** Accessible name of the table (read by screen readers). */
  label: string;
  /** Rendered above the table inside the same card, typically a <FilterBar>. */
  toolbar?: ReactNode;
  /** Set for server-side paging; leave out to page `rows` in the browser. */
  paging?: ServerPaging;
  /** Set for server-side sorting; leave out to sort `rows` in the browser. */
  sorting?: ServerSorting;
  /** Client mode: initial sort. */
  defaultSort?: SortState;
  /** Client mode: initial rows per page. Default 20. */
  pageSize?: number;
  pageSizeOptions?: number[];
  loading?: boolean;
  /** Shown when there are no rows. */
  empty?: ReactNode;
  /** Height limit of the scrolling body (header stays visible). Default 32rem. */
  maxHeight?: string;
  onRowClick?: (row: T) => void;
  className?: string;
}

const alignCls = { left: 'text-left', center: 'text-center', right: 'text-right' } as const;

/**
 * Reusable data table configured by columns (field, header, sortable): optional toolbar (filters), sticky header,
 * sticky right column (actions), row hover, click-to-sort headers, and a footer with "Showing n of total records",
 * rows-per-page and first/previous/pages/next/last. Sorts and pages in the browser by default; pass `sorting`
 * and `paging` to let the server do it.
 */
export function DataGrid<T>({
  columns,
  rows,
  rowKey,
  label,
  toolbar,
  paging,
  sorting,
  defaultSort = null,
  pageSize: initialPageSize = 20,
  pageSizeOptions = [10, 20, 50, 100],
  loading = false,
  empty = 'No records found.',
  maxHeight = '32rem',
  onRowClick,
  className,
}: DataGridProps<T>) {
  // Client-mode state (ignored when the server pages)
  const [clientPageSize, setClientPageSize] = useState(initialPageSize);
  const [clientPage, setClientPage] = useState(1);
  const [clientSort, setClientSort] = useState<SortState>(defaultSort);

  const sort = sorting ? sorting.sort : clientSort;
  const sorted = useMemo(() => {
    if (sorting || !clientSort) return rows; // the server already sorted
    const column = columns.find((c) => c.field === clientSort.field);
    return sortRows(rows, clientSort, (row) =>
      column?.sortValue ? column.sortValue(row) : getValue(row, clientSort.field),
    );
  }, [rows, columns, clientSort, sorting]);

  const server = paging !== undefined;
  const total = server ? paging.total : sorted.length;
  const pageSize = server ? paging.pageSize : clientPageSize;
  const pages = pageCount(total, pageSize);
  const page = Math.min(server ? paging.page : clientPage, pages); // stays valid when rows shrink (e.g. after filtering)
  const first = (page - 1) * pageSize;
  const visible = server ? sorted : sorted.slice(first, first + pageSize);

  const toggleSort = (field: string) => {
    const next = nextSort(sort, field);
    if (sorting) sorting.onSortChange(next);
    else setClientSort(next);
    if (server) paging.onPageChange(1);
    else setClientPage(1);
  };
  const shownSoFar = Math.min(first + visible.length, total);

  const goTo = (p: number) => {
    const next = Math.min(Math.max(1, p), pages);
    if (server) paging.onPageChange(next);
    else setClientPage(next);
  };
  const changeSize = (size: number) => {
    if (server) paging.onPageSizeChange(size);
    else {
      setClientPageSize(size);
      setClientPage(1);
    }
  };

  return (
    <div
      className={cx(
        'w-full overflow-hidden rounded-lg border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800',
        className,
      )}
    >
      {toolbar}

      {/* contain:inline-size stops a wide table from stretching the page; it scrolls inside the grid instead. */}
      <div
        className={cx(
          'relative w-full overflow-auto [contain:inline-size]',
          toolbar ? 'border-t border-gray-200 dark:border-gray-700' : undefined,
        )}
        style={{ maxHeight }}
      >
        <table className="w-full border-separate border-spacing-0 text-sm" aria-busy={loading || undefined}>
          <caption className="sr-only">{label}</caption>
          <thead>
            <tr>
              {columns.map((c) => (
                <th
                  key={c.field}
                  scope="col"
                  aria-sort={
                    sort?.field === c.field
                      ? sort.direction === 'asc'
                        ? 'ascending'
                        : 'descending'
                      : c.sortable
                        ? 'none'
                        : undefined
                  }
                  className={cx(
                    'sticky top-0 z-10 whitespace-nowrap border-b border-gray-200 bg-gray-100 px-6 py-3 font-medium text-gray-900 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-200',
                    alignCls[c.align ?? 'left'],
                    c.stickyRight && 'right-0 z-20 border-l text-center',
                    c.headerClassName,
                  )}
                >
                  {c.sortable ? (
                    <button
                      type="button"
                      onClick={() => toggleSort(c.field)}
                      className="group/sort -mx-1 inline-flex items-center gap-1 rounded px-1 font-medium hover:text-brand-700 focus:outline-none focus-visible:ring-focus dark:hover:text-brand-300"
                    >
                      {c.header}
                      <SortIcon direction={sort?.field === c.field ? sort.direction : undefined} />
                    </button>
                  ) : (
                    c.header
                  )}
                </th>
              ))}
            </tr>
          </thead>
          <tbody
            className={cx(
              'text-gray-800 transition-opacity dark:text-gray-300',
              loading && visible.length > 0 && 'opacity-50',
            )}
          >
            {visible.length === 0 ? (
              <tr>
                <td colSpan={columns.length} className="px-6 py-12 text-center text-gray-500 dark:text-gray-400">
                  {loading ? 'Loading…' : empty}
                </td>
              </tr>
            ) : (
              visible.map((row, i) => (
                <tr
                  key={rowKey(row, first + i)}
                  onClick={onRowClick ? () => onRowClick(row) : undefined}
                  className={cx('group', onRowClick && 'cursor-pointer')}
                >
                  {columns.map((c) => (
                    <td
                      key={c.field}
                      title={c.title?.(row)}
                      className={cx(
                        // Hover colours every cell (not the row) so the sticky column changes too.
                        'whitespace-nowrap border-b border-gray-200 bg-white px-6 py-4 transition-colors group-hover:bg-gray-50 dark:border-gray-700 dark:bg-gray-800 dark:group-hover:bg-gray-700/60',
                        alignCls[c.align ?? 'left'],
                        c.stickyRight && 'sticky right-0 z-[5] border-l text-center',
                        c.className,
                      )}
                    >
                      {c.cell ? c.cell(row) : display(getValue(row, c.field))}
                    </td>
                  ))}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      <div className="flex flex-col gap-3 border-t border-gray-200 px-4 py-3 text-sm text-gray-600 sm:flex-row sm:items-center sm:justify-between dark:border-gray-700 dark:text-gray-400">
        {/* Records up to the end of this page: page 1 → "10 of 13", page 2 → "13 of 13". */}
        <span aria-live="polite">
          Showing {shownSoFar} of {total} {total === 1 ? 'record' : 'records'}
        </span>

        <div className="flex flex-wrap items-center gap-4">
          {pageSizeOptions.length > 0 && (
            <label className="relative inline-flex items-center">
              <span className="sr-only">Rows per page</span>
              <select
                className="cursor-pointer appearance-none rounded-md bg-transparent py-1 pl-2 pr-7 font-medium text-gray-900 hover:bg-gray-100 focus:outline-none focus-visible:ring-focus dark:text-gray-200 dark:hover:bg-gray-700"
                value={pageSize}
                onChange={(e) => changeSize(Number(e.target.value))}
              >
                {pageSizeOptions.map((n) => (
                  <option key={n} value={n}>
                    Show {n}
                  </option>
                ))}
              </select>
              <ChevronDown
                className="pointer-events-none absolute right-1.5 size-4 text-gray-700 dark:text-gray-300"
                aria-hidden="true"
              />
            </label>
          )}

          <nav aria-label={`${label} pages`} className="flex items-center gap-1">
            <PagerButton label="First page" disabled={page === 1} onClick={() => goTo(1)}>
              <ChevronsLeft className="size-4" aria-hidden="true" />
            </PagerButton>
            <PagerButton label="Previous page" disabled={page === 1} onClick={() => goTo(page - 1)}>
              <ChevronLeft className="size-4" aria-hidden="true" />
            </PagerButton>
            {pageWindow(page, pages).map((p) => (
              <PagerButton key={p} label={`Page ${p}`} current={p === page} onClick={() => goTo(p)}>
                {p}
              </PagerButton>
            ))}
            <PagerButton label="Next page" disabled={page === pages} onClick={() => goTo(page + 1)}>
              <ChevronRight className="size-4" aria-hidden="true" />
            </PagerButton>
            <PagerButton label="Last page" disabled={page === pages} onClick={() => goTo(pages)}>
              <ChevronsRight className="size-4" aria-hidden="true" />
            </PagerButton>
          </nav>
        </div>
      </div>
    </div>
  );
}

/** Default cell text for a raw value. */
function display(value: unknown): ReactNode {
  if (value == null || value === '') return <span className="text-gray-400">—</span>;
  if (typeof value === 'boolean') return value ? 'Yes' : 'No';
  return String(value);
}

function SortIcon({ direction }: { direction?: 'asc' | 'desc' }) {
  if (direction === 'asc') return <ArrowUp className="size-3.5" aria-hidden="true" />;
  if (direction === 'desc') return <ArrowDown className="size-3.5" aria-hidden="true" />;
  return <ChevronsUpDown className="size-3.5 text-gray-400 group-hover/sort:text-current" aria-hidden="true" />;
}

function PagerButton({
  children,
  label,
  current,
  disabled,
  onClick,
}: {
  children: ReactNode;
  label: string;
  current?: boolean;
  disabled?: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      aria-label={label}
      aria-current={current ? 'page' : undefined}
      disabled={disabled}
      onClick={onClick}
      className={cx(
        'inline-flex size-9 items-center justify-center rounded-md transition-colors focus:outline-none focus-visible:ring-focus disabled:cursor-not-allowed disabled:text-gray-300 dark:disabled:text-gray-600',
        current
          ? 'bg-gray-100 font-semibold text-gray-900 dark:bg-gray-700 dark:text-white'
          : 'text-gray-700 enabled:hover:bg-gray-100 dark:text-gray-300 dark:enabled:hover:bg-gray-700',
      )}
    >
      {children}
    </button>
  );
}
