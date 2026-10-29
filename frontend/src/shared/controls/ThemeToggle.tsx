import { useEffect, useState } from 'react';
import { Moon, Sun } from 'lucide-react';
import { writeStorage } from '../lib/storage';

const KEY = 'aerialwatch.theme';

/** Light/dark toggle (Windmill style): toggles the `dark` class on <html> and remembers it. */
export function ThemeToggle() {
  const [dark, setDark] = useState(() => document.documentElement.classList.contains('dark'));

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark);
    writeStorage(KEY, dark ? 'dark' : 'light');
  }, [dark]);

  return (
    <button
      type="button"
      className="rounded-md p-1 text-brand-600 focus:outline-none focus-visible:ring-focus dark:text-brand-300"
      aria-label={dark ? 'Switch to light theme' : 'Switch to dark theme'}
      onClick={() => setDark((d) => !d)}
    >
      {dark ? <Sun size={20} aria-hidden="true" /> : <Moon size={20} aria-hidden="true" />}
    </button>
  );
}
