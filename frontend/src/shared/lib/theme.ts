/** Reads a theme colour token (Tailwind v4 exposes them as CSS variables), for libraries that need a real colour. */
export function themeColor(name: string, fallback: string): string {
  if (typeof window === 'undefined') return fallback;
  return getComputedStyle(document.documentElement).getPropertyValue(name).trim() || fallback;
}
