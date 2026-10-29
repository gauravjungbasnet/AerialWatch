import type { ReactNode } from 'react';

interface PageHeaderProps {
  title: string;
  description?: ReactNode;
  actions?: ReactNode;
}

/** Windmill page title (h2, my-6) with optional actions on the right. */
export function PageHeader({ title, description, actions }: PageHeaderProps) {
  return (
    <div className="my-6 flex flex-wrap items-center justify-between gap-4">
      <div>
        <h2 className="text-2xl font-semibold text-gray-700 dark:text-gray-200">{title}</h2>
        {description && <p className="mt-1 text-sm text-gray-600 dark:text-gray-400">{description}</p>}
      </div>
      {actions && <div className="flex flex-wrap gap-2">{actions}</div>}
    </div>
  );
}

/** Smaller section title inside a page (Windmill h4). */
export function SectionTitle({ children }: { children: ReactNode }) {
  return <h4 className="mb-4 text-lg font-semibold text-gray-600 dark:text-gray-300">{children}</h4>;
}
