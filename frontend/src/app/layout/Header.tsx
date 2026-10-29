import { Menu } from 'lucide-react';
import { useLocation } from 'react-router-dom';
import { ButtonLink, ThemeToggle } from '@shared/controls';
import { useActiveApp } from '../useActiveApp';
import { AppSelector } from '../components/AppSelector';
import { CurrentAppLabel } from '../components/CurrentAppLabel';

/** Header: menu button (collapse/expand the sidebar; opens it on mobile), theme toggle and app launcher. */
export function Header({ onMenu, expanded }: { onMenu: () => void; expanded: boolean }) {
  const activeApp = useActiveApp();
  const { pathname } = useLocation();
  const headerItems = activeApp.navigation
    .flatMap((section) => section.items)
    .filter((item) => item.placement === 'header');
  return (
    <header className="z-40 h-16 shrink-0 border-b border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
      <div className="flex h-full items-center gap-3 px-4 sm:px-6">
        <button
          type="button"
          onClick={onMenu}
          aria-label={expanded ? 'Collapse menu' : 'Expand menu'}
          aria-expanded={expanded}
          aria-controls="primary-sidebar"
          className="-ml-2 inline-flex size-10 shrink-0 items-center justify-center rounded-md text-gray-800 hover:bg-gray-100 focus:outline-none focus-visible:ring-focus dark:text-gray-200 dark:hover:bg-gray-700"
        >
          <Menu className="size-6" aria-hidden="true" />
        </button>
        <div className="min-w-0 md:hidden">
          <CurrentAppLabel />
        </div>
        <div className="ml-auto flex shrink-0 items-center gap-3 text-brand-600 dark:text-brand-300">
          {headerItems.map((item) => (
            <div key={item.to} className="group relative">
              <ButtonLink
                to={item.to}
                variant="ghost"
                className="size-11 shrink-0 p-0! text-brand-600 dark:text-brand-300"
                aria-label={item.label}
                aria-current={(item.isActive ? item.isActive(pathname) : pathname === item.to) ? 'page' : undefined}
              >
                {item.icon}
              </ButtonLink>
              <span
                aria-hidden="true"
                className="pointer-events-none absolute right-0 top-full z-10 mt-2 whitespace-nowrap rounded-md bg-gray-900 px-2 py-1 text-xs text-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100 dark:bg-gray-100 dark:text-gray-900"
              >
                {item.label}
              </span>
            </div>
          ))}
          <ThemeToggle />
          <AppSelector />
        </div>
      </div>
    </header>
  );
}
