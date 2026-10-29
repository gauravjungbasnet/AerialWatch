import { useEffect, useState } from 'react';

/**
 * The value, but only after it has stopped changing for `delayMs`. Use it before server-side searches so typing
 * "katherine" sends one request, not nine.
 */
export function useDebouncedValue<T>(value: T, delayMs = 300): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}
