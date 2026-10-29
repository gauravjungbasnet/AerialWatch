import type { ReactNode } from 'react';
import { cx } from '@shared/lib/format';

/** One photo pane; a small label says which camera it is and whether it counts (D-039). */
export function ViewerPanel({
  label,
  muted = false,
  children,
}: {
  label?: string;
  muted?: boolean;
  children: ReactNode;
}) {
  return (
    <div className="relative size-full min-w-0">
      {children}
      {label && (
        <span
          className={cx(
            'pointer-events-none absolute left-2 top-2 z-10 rounded-md px-2 py-1 text-xs font-semibold shadow',
            muted ? 'bg-gray-900/80 text-gray-100' : 'bg-brand-600 text-white',
          )}
        >
          {label}
        </span>
      )}
    </div>
  );
}
