import type { ReactNode } from 'react';
import { cx } from '../lib/format';

export type IconColor = 'brand' | 'orange' | 'green' | 'blue' | 'teal';

const iconColor: Record<IconColor, string> = {
  brand: 'text-brand-500 bg-brand-100 dark:text-brand-100 dark:bg-brand-500',
  orange: 'text-orange-500 bg-orange-100 dark:text-orange-100 dark:bg-orange-500',
  green: 'text-green-500 bg-green-100 dark:text-green-100 dark:bg-green-500',
  blue: 'text-blue-500 bg-blue-100 dark:text-blue-100 dark:bg-blue-500',
  teal: 'text-teal-500 bg-teal-100 dark:text-teal-100 dark:bg-teal-500',
};

/** Windmill RoundIcon. */
export function RoundIcon({ icon, color = 'brand' }: { icon: ReactNode; color?: IconColor }) {
  return <div className={cx('shrink-0 rounded-full p-3', iconColor[color])}>{icon}</div>;
}

interface InfoCardProps {
  title: string;
  value: ReactNode;
  icon?: ReactNode;
  color?: IconColor;
  sub?: ReactNode;
  /** Emphasised version for the headline result. */
  highlight?: boolean;
  className?: string;
}

/** Windmill InfoCard: round icon + label + value. */
export function InfoCard({ title, value, icon, color = 'brand', sub, highlight, className }: InfoCardProps) {
  return (
    <div
      className={cx(
        'flex min-w-0 items-center gap-4 rounded-lg p-4',
        highlight ? 'bg-brand-600 text-white shadow-md' : 'bg-white shadow-card dark:bg-gray-800',
        className,
      )}
    >
      {icon &&
        (highlight ? (
          <div className="shrink-0 rounded-full bg-white/20 p-3 text-white">{icon}</div>
        ) : (
          <RoundIcon icon={icon} color={color} />
        ))}
      <div className="min-w-0">
        <p
          className={cx('mb-1 text-sm font-medium', highlight ? 'text-brand-100' : 'text-gray-600 dark:text-gray-400')}
        >
          {title}
        </p>
        <p
          className={cx(
            'font-semibold',
            highlight ? 'text-2xl text-white' : 'text-lg text-gray-700 dark:text-gray-200',
          )}
        >
          {value}
        </p>
        {sub && (
          <p className={cx('mt-0.5 text-xs', highlight ? 'text-brand-100' : 'text-gray-500 dark:text-gray-400')}>
            {sub}
          </p>
        )}
      </div>
    </div>
  );
}
