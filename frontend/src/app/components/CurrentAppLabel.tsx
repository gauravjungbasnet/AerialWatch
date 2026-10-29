import { useActiveApp } from '../useActiveApp';

/** Module identity comes from the same registry as the launcher and routes. */
export function CurrentAppLabel() {
  const activeApp = useActiveApp();
  const Icon = activeApp.icon;
  return (
    <span
      aria-label={`Current app: ${activeApp.name}`}
      className="inline-flex items-center gap-2 text-xs font-medium text-brand-700 dark:text-brand-300"
    >
      <Icon className="size-4 shrink-0" aria-hidden="true" />
      {activeApp.name}
    </span>
  );
}
