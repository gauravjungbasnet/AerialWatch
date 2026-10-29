import type { ReactNode, TdHTMLAttributes } from 'react';
import { cx } from '../lib/format';

/** Windmill TableContainer: rounded, hairline ring, horizontal scroll on small screens. */
export function TableContainer({ children, className }: { children: ReactNode; className?: string }) {
  return (
    <div className={cx('w-full overflow-hidden rounded-lg shadow-card', className)}>
      <div className="w-full overflow-x-auto">{children}</div>
    </div>
  );
}

export function Table({ children }: { children: ReactNode }) {
  return <table className="w-full whitespace-nowrap">{children}</table>;
}

export function TableHeader({ children }: { children: ReactNode }) {
  return (
    <thead className="border-b bg-gray-50 text-left text-xs font-semibold uppercase tracking-wide text-gray-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400">
      {children}
    </thead>
  );
}

export function TableBody({ children }: { children: ReactNode }) {
  return (
    <tbody className="divide-y bg-white text-gray-700 dark:divide-gray-700 dark:bg-gray-800 dark:text-gray-400">
      {children}
    </tbody>
  );
}

export function TableRow({ children, className }: { children: ReactNode; className?: string }) {
  return <tr className={className}>{children}</tr>;
}

export function TableHeadCell({ children, className }: { children?: ReactNode; className?: string }) {
  return (
    <th scope="col" className={cx('px-4 py-3', className)}>
      {children}
    </th>
  );
}

export function TableCell({ children, className, ...rest }: TdHTMLAttributes<HTMLTableCellElement>) {
  return (
    <td className={cx('px-4 py-3 text-sm', className)} {...rest}>
      {children}
    </td>
  );
}

/** Footer row under a table (e.g. counts). */
export function TableFooter({ children }: { children: ReactNode }) {
  return (
    <div className="border-t bg-gray-50 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:border-gray-700 dark:bg-gray-800 dark:text-gray-400">
      {children}
    </div>
  );
}
