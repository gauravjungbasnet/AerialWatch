import type { ReactNode } from 'react';
import { AlertTriangle, CheckCircle2, Info, XCircle } from 'lucide-react';
import { ApiError } from '../api/client';
import { cx } from '../lib/format';

export type Tone = 'success' | 'danger' | 'warning' | 'neutral' | 'primary' | 'info';

// ---- Alert (Windmill Alert) ----
const alertTone = {
  info: {
    cls: 'bg-blue-50 text-blue-900 dark:bg-blue-600/90 dark:text-white',
    icon: Info,
    iconCls: 'text-blue-600 dark:text-white',
  },
  success: {
    cls: 'bg-green-50 text-green-900 dark:bg-green-600/90 dark:text-white',
    icon: CheckCircle2,
    iconCls: 'text-green-600 dark:text-white',
  },
  warning: {
    cls: 'bg-yellow-50 text-yellow-900 dark:bg-yellow-600/90 dark:text-white',
    icon: AlertTriangle,
    iconCls: 'text-yellow-600 dark:text-white',
  },
  danger: {
    cls: 'bg-red-50 text-red-900 dark:bg-red-600/90 dark:text-white',
    icon: XCircle,
    iconCls: 'text-red-600 dark:text-white',
  },
} as const;

interface AlertProps {
  tone?: keyof typeof alertTone;
  title?: ReactNode;
  children?: ReactNode;
  className?: string;
}

export function Alert({ tone = 'info', title, children, className }: AlertProps) {
  const t = alertTone[tone];
  const Icon = t.icon;
  return (
    <div
      role={tone === 'danger' ? 'alert' : 'status'}
      className={cx('relative rounded-lg p-4 pl-12 text-sm leading-5', t.cls, className)}
    >
      <Icon size={20} className={cx('absolute left-4 top-4', t.iconCls)} aria-hidden="true" />
      {title && <p className="font-semibold">{title}</p>}
      {children && <div className={title ? 'mt-1' : undefined}>{children}</div>}
    </div>
  );
}

/** Shows the message from an ApiError (or a generic one). */
export function ErrorAlert({ error, title = 'Something went wrong' }: { error: unknown; title?: string }) {
  const message = error instanceof ApiError ? error.message : 'Please try again.';
  return (
    <Alert tone="danger" title={title}>
      {message}
    </Alert>
  );
}

// ---- Badge (Windmill Badge) ----
const badgeTone: Record<Tone, string> = {
  success: 'text-green-700 bg-green-100 dark:bg-green-700 dark:text-green-100',
  danger: 'text-red-700 bg-red-100 dark:text-red-100 dark:bg-red-700',
  warning: 'text-orange-700 bg-orange-100 dark:text-white dark:bg-orange-600',
  neutral: 'text-gray-700 bg-gray-100 dark:text-gray-100 dark:bg-gray-700',
  primary: 'text-brand-700 bg-brand-100 dark:text-white dark:bg-brand-600',
  info: 'text-blue-700 bg-blue-100 dark:text-white dark:bg-blue-600',
};

export function Badge({
  tone = 'neutral',
  children,
  className,
}: {
  tone?: Tone;
  children: ReactNode;
  className?: string;
}) {
  return (
    <span
      className={cx(
        'inline-flex items-center rounded-full px-2 py-1 text-xs font-semibold leading-tight',
        badgeTone[tone],
        className,
      )}
    >
      {children}
    </span>
  );
}

// ---- Loading ----
export function Spinner({ label = 'Loading…', className }: { label?: string; className?: string }) {
  return (
    <span
      className={cx('inline-flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400', className)}
      role="status"
    >
      <span
        className="size-4 animate-spin rounded-full border-2 border-brand-600 border-r-transparent"
        aria-hidden="true"
      />
      {label}
    </span>
  );
}

export function SkeletonRows({ rows = 4 }: { rows?: number }) {
  return (
    <div className="flex flex-col gap-3" aria-hidden="true">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="h-8 w-full animate-pulse rounded bg-gray-100 dark:bg-gray-700" />
      ))}
    </div>
  );
}

// ---- Empty state ----
interface EmptyStateProps {
  title: string;
  description?: ReactNode;
  action?: ReactNode;
}

export function EmptyState({ title, description, action }: EmptyStateProps) {
  return (
    <div className="px-4 py-10 text-center">
      <p className="font-semibold text-gray-700 dark:text-gray-200">{title}</p>
      {description && <p className="mx-auto mt-1 max-w-md text-sm text-gray-600 dark:text-gray-400">{description}</p>}
      {action && <div className="mt-4">{action}</div>}
    </div>
  );
}

export function Kbd({ children }: { children: ReactNode }) {
  return (
    <kbd className="rounded border border-gray-300 bg-gray-50 px-1.5 py-0.5 font-mono text-xs text-gray-700 dark:border-gray-600 dark:bg-gray-700 dark:text-gray-300">
      {children}
    </kbd>
  );
}
