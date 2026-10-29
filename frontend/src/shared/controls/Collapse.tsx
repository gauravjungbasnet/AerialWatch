import type { ReactNode } from 'react';
import { ChevronDown } from 'lucide-react';
import { cx } from '../lib/format';

interface CollapseProps {
  title: ReactNode;
  children: ReactNode;
  defaultOpen?: boolean;
  onToggle?: (open: boolean) => void;
  /** Render as its own card. */
  bordered?: boolean;
  className?: string;
}

/** Expandable section on native <details> (keyboard accessible). */
export function Collapse({ title, children, defaultOpen, onToggle, bordered = false, className }: CollapseProps) {
  return (
    <details
      className={cx('group', bordered && 'rounded-lg bg-white shadow-card dark:bg-gray-800', className)}
      open={defaultOpen}
      onToggle={(e) => onToggle?.((e.currentTarget as HTMLDetailsElement).open)}
    >
      <summary
        className={cx(
          'flex cursor-pointer list-none items-center justify-between gap-3 text-sm font-medium text-brand-600 dark:text-brand-300 [&::-webkit-details-marker]:hidden',
          bordered ? 'px-4 py-4 sm:px-6 text-gray-800 dark:text-gray-300' : 'py-1',
        )}
      >
        <span>{title}</span>
        <ChevronDown size={16} className="shrink-0 transition-transform group-open:rotate-180" aria-hidden="true" />
      </summary>
      <div className={cx('text-sm', bordered ? 'px-4 pb-6 sm:px-6' : 'pt-2')}>{children}</div>
    </details>
  );
}
