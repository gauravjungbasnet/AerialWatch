import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Button } from '@shared/controls';

import { apps } from '../registry';
import { useActiveApp } from '../useActiveApp';

/** Compact app launcher; future modules remain clearly labelled as coming soon. */
export function AppSelector() {
  const activeApp = useActiveApp();
  const [open, setOpen] = useState(false);
  const container = useRef<HTMLDivElement>(null);
  const trigger = useRef<HTMLSpanElement>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (!open) return;
    const dismiss = (event: PointerEvent) => {
      if (!container.current?.contains(event.target as Node)) setOpen(false);
    };
    const escape = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        setOpen(false);
        trigger.current?.querySelector('button')?.focus();
      }
    };
    document.addEventListener('pointerdown', dismiss);
    document.addEventListener('keydown', escape);
    return () => {
      document.removeEventListener('pointerdown', dismiss);
      document.removeEventListener('keydown', escape);
    };
  }, [open]);

  return (
    <div
      ref={container}
      className="relative"
      onBlur={(event) => {
        if (!event.currentTarget.contains(event.relatedTarget)) setOpen(false);
      }}
    >
      <span ref={trigger}>
        <Button
          variant="ghost"
          className="size-11 shrink-0 rounded-full p-0! text-gray-700 dark:text-gray-200"
          aria-label="Select app"
          title="Apps"
          aria-expanded={open}
          aria-controls="app-selector"
          onClick={() => setOpen(!open)}
        >
          <svg className="size-7 shrink-0" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
            {[5, 12, 19].flatMap((cy) => [5, 12, 19].map((cx) => <circle key={`${cx}-${cy}`} cx={cx} cy={cy} r="2" />))}
          </svg>
        </Button>
      </span>
      {open && (
        <section
          id="app-selector"
          aria-label="Aerial Watch apps"
          className="absolute right-0 top-full mt-3 w-72 max-w-[calc(100vw-3rem)] rounded-2xl border border-gray-200 bg-white p-4 shadow-xl dark:border-gray-700 dark:bg-gray-800"
        >
          <h2 className="mb-4 text-sm font-semibold text-gray-700 dark:text-gray-200">Aerial Watch apps</h2>
          <div className="grid grid-cols-3 gap-2">
            {apps.map(({ code, name, icon: Icon, available, home }) => (
              <div key={code} className="group relative">
                <Button
                  variant="ghost"
                  title={available ? name : `${name} — Coming soon`}
                  aria-label={`${name}, ${code === activeApp.code ? 'current app' : available ? 'open app' : 'coming soon'}`}
                  aria-current={code === activeApp.code ? 'true' : undefined}
                  className={`min-h-24 w-full flex-col gap-2 px-1! py-3 ${code === activeApp.code ? 'border-brand-600! dark:border-brand-300!' : ''}`}
                  onClick={() => {
                    navigate(home);
                    setOpen(false);
                  }}
                >
                  <span className="flex size-11 items-center justify-center rounded-2xl bg-brand-100 text-brand-700 dark:bg-brand-900 dark:text-brand-200">
                    <Icon className="size-6" aria-hidden="true" />
                  </span>
                  <span className="font-semibold text-gray-800 dark:text-gray-100">{code}</span>
                </Button>
                <span
                  aria-hidden="true"
                  className="pointer-events-none absolute left-1/2 top-full z-10 mt-1 -translate-x-1/2 whitespace-nowrap rounded-md bg-gray-900 px-2 py-1 text-xs text-white opacity-0 transition-opacity group-hover:opacity-100 group-focus-within:opacity-100 dark:bg-gray-100 dark:text-gray-900"
                >
                  {name}
                  {!available && ' — Coming soon'}
                </span>
              </div>
            ))}
          </div>
        </section>
      )}
    </div>
  );
}
