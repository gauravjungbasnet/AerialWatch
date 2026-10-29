import { Link } from 'react-router-dom';
import { cx } from '@shared/lib/format';
import { useActiveApp } from '../useActiveApp';
import { SideNavigation } from '@shared/components/navigation/SideNavigation';
import { CurrentAppLabel } from '../components/CurrentAppLabel';

/**
 * Sidebar content: brand row (same height as the header, so their bottom borders line up), menu, primary action.
 * Collapsed = icons only, with the label as a tooltip.
 */
export function SidebarContent({ onNavigate, collapsed = false }: { onNavigate?: () => void; collapsed?: boolean }) {
  const activeApp = useActiveApp();
  const AppIcon = activeApp.icon;

  return (
    <div className="flex h-full flex-col text-gray-500 dark:text-gray-400">
      <Link
        to={activeApp.home}
        onClick={onNavigate}
        title={collapsed ? `Aerial Watch · ${activeApp.name}` : undefined}
        className={cx(
          'flex h-16 shrink-0 items-center border-b border-gray-200 font-bold text-gray-800 dark:border-gray-700 dark:text-gray-200',
          collapsed ? 'justify-center' : 'px-6',
        )}
      >
        {collapsed ? (
          <AppIcon
            className="size-6 text-brand-600 dark:text-brand-300"
            aria-label={`Aerial Watch · ${activeApp.name}`}
          />
        ) : (
          <span className="leading-tight">
            <span className="block text-lg">Aerial Watch</span>
            <CurrentAppLabel />
          </span>
        )}
      </Link>

      <SideNavigation sections={activeApp.navigation} onNavigate={onNavigate} collapsed={collapsed} />

      {/* {activeApp.primaryAction && (
        <div className={cx('my-6', collapsed ? 'px-3' : 'px-6')}>
          <ButtonLink
            to={activeApp.primaryAction.to}
            block
            onClick={onNavigate}
            icon={<Plus size={16} aria-hidden="true" />}
            title={collapsed ? activeApp.primaryAction.label : undefined}
            aria-label={collapsed ? activeApp.primaryAction.label : undefined}
          >
            {!collapsed && activeApp.primaryAction.label}
          </ButtonLink>
        </div>
      )} */}

      {!collapsed && (
        <p className="mt-auto px-6 pb-4 text-xs leading-relaxed text-gray-400 dark:text-gray-500">
          Student project · CDU IT Code Fair 2026. Not endorsed by NACAS or CDU.
        </p>
      )}
    </div>
  );
}
