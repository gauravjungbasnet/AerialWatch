import { Link, useLocation } from 'react-router-dom';
import { cx } from '../../lib/format';
import type { NavSection } from './types';

/** Reusable navigation renderer; each app supplies its own sections. Collapsed shows icons with tooltips. */
export function SideNavigation({
  sections,
  onNavigate,
  collapsed = false,
}: {
  sections: NavSection[];
  onNavigate?: () => void;
  collapsed?: boolean;
}) {
  const { pathname } = useLocation();
  return (
    <nav aria-label="App navigation">
      {sections
        .map((section) => ({ ...section, items: section.items.filter((item) => item.placement !== 'header') }))
        .filter((section) => section.items.length > 0)
        .map((section, i) => (
          <div key={section.title ?? i} className={i === 0 ? 'mt-4' : 'mt-4'}>
            {section.title &&
              (collapsed ? (
                <hr className="mx-4 mb-2 border-gray-200 dark:border-gray-700" aria-label={section.title} />
              ) : (
                <p className="px-6 pb-1 text-xs font-semibold uppercase tracking-wide text-gray-400 dark:text-gray-500">
                  {section.title}
                </p>
              ))}
            <ul>
              {section.items.map((item) => {
                const active = item.isActive ? item.isActive(pathname) : pathname === item.to;
                return (
                  <li key={item.to} className={cx('relative py-1', collapsed ? 'px-3' : 'px-3')}>
                    {active && (
                      <span className="absolute inset-y-1 left-0 w-1 rounded-r-lg bg-brand-600" aria-hidden="true" />
                    )}
                    <Link
                      to={item.to}
                      onClick={onNavigate}
                      aria-current={active ? 'page' : undefined}
                      title={collapsed ? item.label : undefined}
                      className={cx(
                        'flex min-h-11 items-center rounded-md text-sm font-semibold transition-colors duration-150 hover:bg-gray-50 hover:text-gray-800 focus:outline-none focus-visible:ring-focus dark:hover:bg-gray-700 dark:hover:text-gray-200',
                        collapsed ? 'justify-center' : 'px-3',
                        active && 'text-brand-700 dark:text-brand-300',
                      )}
                    >
                      {item.icon}
                      <span className={collapsed ? 'sr-only' : 'ml-4'}>{item.label}</span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
    </nav>
  );
}
