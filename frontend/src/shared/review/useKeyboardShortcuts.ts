import { useEffect, useRef } from 'react';

/** True when the key press belongs to a form field or a focused control, which keeps its normal behaviour. */
export function isTyping(e: KeyboardEvent): boolean {
  const t = e.target as HTMLElement | null;
  if (!t) return false;
  if (t.isContentEditable || ['INPUT', 'TEXTAREA', 'SELECT'].includes(t.tagName)) return true;
  // Space/Enter on a focused button or link activates it; don't also run the shortcut.
  return e.key === ' ' && (t.tagName === 'BUTTON' || t.tagName === 'A');
}

interface ShortcutOptions<A extends string> {
  /** Turn the shortcuts off, e.g. before a review has started. */
  enabled: boolean;
  /** Actions that must not repeat while the key is held down (the default browser repeat is swallowed). */
  noRepeat?: A[];
}

/**
 * Page-wide keyboard shortcuts for any workspace (docs/18 step 4). The caller maps a key press to an action name
 * (`resolve`, null = not a shortcut) and handles the action. Ignored while typing or while a dialog is open.
 */
export function useKeyboardShortcuts<A extends string>(
  resolve: (e: KeyboardEvent) => A | null,
  handler: (action: A) => void,
  { enabled, noRepeat = [] }: ShortcutOptions<A>,
) {
  const latest = useRef({ resolve, handler, noRepeat });
  latest.current = { resolve, handler, noRepeat };

  useEffect(() => {
    if (!enabled) return;
    const onKey = (e: KeyboardEvent) => {
      if (isTyping(e) || document.querySelector('[role="dialog"], dialog[open]')) return;
      const action = latest.current.resolve(e);
      if (!action) return;
      e.preventDefault();
      if (e.repeat && latest.current.noRepeat.includes(action)) return;
      latest.current.handler(action);
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [enabled]);
}
