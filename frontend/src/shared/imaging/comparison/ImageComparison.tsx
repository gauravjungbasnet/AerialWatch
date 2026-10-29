import type { ReactNode } from 'react';
import { SegmentedControl } from '../../controls/SegmentedControl';
import { cx } from '../../lib/format';

export interface ComparisonPane {
  id: string;
  label: string;
  content: ReactNode;
}
interface ImageComparisonProps {
  name: string;
  panes: ComparisonPane[];
  view: string;
  onView(view: string): void;
  layout?: 'workspace' | 'gallery';
  actions?: ReactNode;
}

/** Neutral camera-independent comparison: callers supply panes, labels and per-pane viewer configuration. */
export function ImageComparison({ name, panes, view, onView, layout = 'gallery', actions }: ImageComparisonProps) {
  const visible = view === 'both' || !panes.some((p) => p.id === view) ? panes : panes.filter((p) => p.id === view);
  const paired = visible.length > 1;
  return (
    <div className="min-w-0">
      {(panes.length > 1 || actions) && (
        <div className="flex flex-wrap items-end justify-between gap-3 border-b border-gray-200 p-3 dark:border-gray-700">
          {panes.length > 1 && (
            <SegmentedControl
              name={name}
              legend="Image comparison"
              variant="tabs"
              value={view}
              onChange={onView}
              options={[
                { value: 'both', label: 'Side by side' },
                ...panes.map((p) => ({ value: p.id, label: p.label })),
              ]}
            />
          )}
          {actions && <div className="ml-auto">{actions}</div>}
        </div>
      )}
      <div
        className={cx(
          layout === 'workspace'
            ? paired
              ? 'grid h-[40rem] grid-cols-1 grid-rows-2 gap-px bg-gray-700 md:h-[min(60vh,34rem)] md:min-h-[24rem] md:grid-cols-2 md:grid-rows-1'
              : 'h-[min(60vh,34rem)] min-h-[24rem]'
            : 'grid gap-5 rounded-2xl bg-gray-50 p-3 sm:p-4 dark:bg-gray-900/40',
          layout === 'gallery' && paired && 'sm:grid-cols-2',
        )}
      >
        {visible.map((pane) => (
          <div key={pane.id} className="min-h-0 min-w-0 h-full">
            {pane.content}
          </div>
        ))}
      </div>
    </div>
  );
}
