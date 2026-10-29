import { useEffect, useState } from 'react';
import { matchRoutes, Outlet, useLocation } from 'react-router-dom';
import type { AppRoute } from '@shared/types/routes';
import { useActiveApp } from '../useActiveApp';
import { cx } from '@shared/lib/format';
import { readStorage, writeStorage } from '@shared/lib/storage';
import { SidebarContent } from './Sidebar';
import { Header } from './Header';

const COLLAPSED_KEY = 'aerialwatch.sidebarCollapsed';
const isDesktop = () => window.matchMedia('(min-width: 768px)').matches; // Tailwind md

/**
 * App shell: sidebar (full width or collapsed to icons on desktop, slide-over on mobile), header with the menu
 * button, scrollable main. A solid border separates sidebar, header and content.
 */
export function AppLayout() {
  const [open, setOpen] = useState(false); // mobile drawer
  const [collapsed, setCollapsed] = useState(() => readStorage(COLLAPSED_KEY) === 'true'); // desktop, remembered
  const { pathname } = useLocation();
  const activeApp = useActiveApp();
  const fullBleed = matchRoutes<AppRoute>([{ path: activeApp.home, children: activeApp.routes }], pathname)?.some(
    (match) => match.route.handle?.fullBleed === true,
  );

  useEffect(() => setOpen(false), [pathname]);

  useEffect(() => {
    if (!open) return;
    const onKey = (e: KeyboardEvent) => e.key === 'Escape' && setOpen(false);
    document.addEventListener('keydown', onKey);
    return () => document.removeEventListener('keydown', onKey);
  }, [open]);

  const toggleMenu = () => {
    if (isDesktop()) {
      setCollapsed((c) => {
        writeStorage(COLLAPSED_KEY, String(!c));
        return !c;
      });
    } else {
      setOpen((o) => !o);
    }
  };

  return (
    <div className="flex h-dvh overflow-hidden bg-gray-50 dark:bg-gray-900">
      <a
        href="#main"
        className="sr-only focus:not-sr-only focus:absolute focus:z-50 focus:m-2 focus:rounded focus:bg-white focus:px-3 focus:py-2"
      >
        Skip to content
      </a>

      {/* Desktop sidebar */}
      <aside
        id="primary-sidebar"
        aria-label="Primary"
        className={cx(
          'z-30 hidden shrink-0 overflow-y-auto overflow-x-hidden border-r border-gray-200 bg-white transition-[width] duration-200 md:block dark:border-gray-700 dark:bg-gray-800',
          collapsed ? 'w-20' : 'w-64',
        )}
      >
        <SidebarContent collapsed={collapsed} />
      </aside>

      {/* Mobile sidebar */}
      {open && (
        <>
          <div className="fixed inset-0 z-10 bg-black/50 md:hidden" onClick={() => setOpen(false)} aria-hidden="true" />
          <aside
            aria-label="Primary"
            className="fixed inset-y-0 z-20 w-64 shrink-0 overflow-y-auto border-r border-gray-200 bg-white md:hidden dark:border-gray-700 dark:bg-gray-800"
          >
            <SidebarContent onNavigate={() => setOpen(false)} />
          </aside>
        </>
      )}

      <div className="flex w-full min-w-0 flex-1 flex-col overflow-hidden">
        <Header onMenu={toggleMenu} expanded={open || !collapsed} />
        <main id="main" className="min-h-0 min-w-0 flex-1 overflow-y-auto">
          <div
            className={fullBleed ? 'h-full min-w-0' : 'container mx-auto grid min-w-0 grid-cols-1 max-w-8xl px-6 pb-10'}
          >
            <Outlet />
          </div>
        </main>
      </div>
    </div>
  );
}
