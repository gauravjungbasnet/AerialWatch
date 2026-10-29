/**
 * Every date and time the app shows goes through here (docs/18 step 6), so all screens agree: times are NT time
 * (Australia/Darwin) whatever the browser's zone, and date-only values ("2026-10-20") never shift a day.
 */
const NT = 'Australia/Darwin';
const LOCALE = 'en-AU';

/** Today's date as yyyy-MM-dd in the browser's local time (for date inputs). */
export function todayIso(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
}

/** Time of day of a timestamp in NT time, e.g. "18:21:05". */
export function ntTime(iso: string): string {
  return new Date(iso).toLocaleTimeString(LOCALE, { timeZone: NT, hour12: false });
}

/** Date of a timestamp in NT time: "20/10/2026", or e.g. { day: 'numeric', month: 'short', year: 'numeric' }. */
export function ntDate(iso: string, options: Intl.DateTimeFormatOptions = {}): string {
  return new Date(iso).toLocaleDateString(LOCALE, { ...options, timeZone: NT });
}

/** Date and time of a timestamp in NT time, e.g. "20/10/2026, 18:21:05". */
export function ntDateTime(iso: string): string {
  return new Date(iso).toLocaleString(LOCALE, { timeZone: NT, hour12: false });
}

/** A date-only value ("2026-10-20") as text, e.g. "Tue, 20 Oct 2026"; no time zone can move it to another day. */
export function calendarDate(isoDate: string, options: Intl.DateTimeFormatOptions = {}): string {
  const [y, m, d] = isoDate.split('-').map(Number);
  return new Date(Date.UTC(y, m - 1, d)).toLocaleDateString(LOCALE, { ...options, timeZone: 'UTC' });
}
