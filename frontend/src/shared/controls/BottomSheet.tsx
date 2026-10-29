import { useEffect, useId, useRef, type ReactNode } from 'react';
import { X } from 'lucide-react';
import { Button } from './Button';
import { cx } from '../lib/format';

interface BottomSheetProps {
  open: boolean;
  title: string;
  onClose: () => void;
  children: ReactNode;
  actions?: ReactNode;
  className?: string;
  titleIcon?: ReactNode;
  showHandle?: boolean;
}

/** Non-modal sheet: the map stays usable while photo evidence is open. */
export function BottomSheet({
  open,
  title,
  onClose,
  children,
  actions,
  className,
  titleIcon,
  showHandle,
}: BottomSheetProps) {
  const titleId = useId();
  const heading = useRef<HTMLHeadingElement>(null);
  const close = useRef(onClose);
  close.current = onClose;

  useEffect(() => {
    if (!open) return;
    const previous = document.activeElement as HTMLElement | null;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === 'Escape' && !document.querySelector('dialog[open]')) {
        event.preventDefault();
        close.current();
      }
    };
    document.addEventListener('keydown', onKey);
    return () => {
      document.removeEventListener('keydown', onKey);
      if (previous?.isConnected) previous.focus({ preventScroll: true });
    };
  }, [open]);

  useEffect(() => {
    if (open) heading.current?.focus({ preventScroll: true });
  }, [open, title]);

  if (!open) return null;
  return (
    <section
      aria-labelledby={titleId}
      className={cx(
        'flex max-h-[55dvh] min-w-0 flex-col overflow-hidden rounded-t-2xl border border-gray-200 bg-white shadow-xl dark:border-gray-700 dark:bg-gray-800',
        className,
      )}
    >
      {showHandle && (
        <div className="flex shrink-0 justify-center pb-1 pt-3" aria-hidden="true">
          <span className="h-1.5 w-12 rounded-full bg-gray-200 dark:bg-gray-600" />
        </div>
      )}
      <header className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b border-gray-200 px-4 py-2 dark:border-gray-700">
        <h2
          ref={heading}
          id={titleId}
          tabIndex={-1}
          aria-live="polite"
          className="flex min-w-0 flex-1 items-center gap-2 text-sm font-semibold text-gray-900 outline-none dark:text-gray-100"
        >
          {titleIcon}
          <span className="truncate" title={title}>
            {title}
          </span>
        </h2>
        <div className="ml-auto flex items-center gap-2">
          {actions}
          <Button variant="ghost" className="size-11 p-0!" aria-label="Close photos" onClick={onClose}>
            <X size={18} aria-hidden="true" />
          </Button>
        </div>
      </header>
      <div className="min-h-0 overflow-y-auto overscroll-contain p-3 sm:p-4">{children}</div>
    </section>
  );
}
