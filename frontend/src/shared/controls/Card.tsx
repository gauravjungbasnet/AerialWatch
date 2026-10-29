import type { ReactNode } from 'react';
import { cx } from '../lib/format';

interface CardProps {
  title?: ReactNode;
  description?: ReactNode;
  actions?: ReactNode;
  children?: ReactNode;
  className?: string;
  bodyClassName?: string;
  id?: string;
}

/** Card: white surface, hairline border, soft shadow, rounded-xl (planner design, D-032). */
export function Card({ title, description, actions, children, className, bodyClassName, id }: CardProps) {
  const headingId = id ? `${id}-title` : undefined;
  return (
    <section
      id={id}
      aria-labelledby={headingId}
      className={cx(
        'min-w-0 rounded-xl border border-gray-100 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-800',
        className,
      )}
    >
      <div className={cx('p-5 md:p-6', bodyClassName)}>
        {(title || actions) && (
          <div className="mb-4 flex flex-wrap items-start justify-between gap-3">
            <div>
              {title && (
                <h3 id={headingId} className="text-base font-semibold text-gray-900 dark:text-gray-200">
                  {title}
                </h3>
              )}
              {description && <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">{description}</p>}
            </div>
            {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
          </div>
        )}
        {children}
      </div>
    </section>
  );
}
