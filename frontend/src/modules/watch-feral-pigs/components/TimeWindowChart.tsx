import type { PlannerWindow } from '../api/types';
import { cx, percent } from '@shared/lib/format';
import { Badge } from '@shared/controls';

/** Horizontal bars: chance a pig is visible in each study time window. */
export function TimeWindowChart({ windows }: { windows: PlannerWindow[] }) {
  return (
    <ul className="space-y-5" aria-label="Chance a pig is visible, by time of day">
      {windows.map((w) => (
        <li key={w.window}>
          <div className="mb-1.5 flex items-baseline justify-between gap-3 text-sm">
            <span>
              <span className="font-medium text-gray-800 dark:text-gray-200">{w.label}</span>
              <span className="ml-2 text-xs text-gray-400">{w.studyHours}</span>
            </span>
            <span className="flex items-center gap-2">
              {w.isBest && <Badge tone="success">Best</Badge>}
              {w.isWorst && <Badge tone="danger">Worst</Badge>}
              <span className="w-10 text-right font-bold tabular-nums text-gray-900 dark:text-gray-100">
                {percent(w.probability)}
              </span>
            </span>
          </div>
          <div className="h-2 overflow-hidden rounded-full bg-gray-100 dark:bg-gray-700" aria-hidden="true">
            <div
              className={cx(
                'h-full rounded-full transition-[width] duration-500',
                w.isBest ? 'bg-brand-600' : w.isWorst ? 'bg-red-400' : 'bg-brand-300',
              )}
              style={{ width: `${Math.max(w.probability * 100, 2)}%` }}
            />
          </div>
          {w.note && <p className="mt-1 text-xs text-orange-700 dark:text-orange-300">{w.note}</p>}
        </li>
      ))}
    </ul>
  );
}
