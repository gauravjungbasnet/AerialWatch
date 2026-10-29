/** 0.7213 → "72%" */
export const percent = (p: number) => `${Math.round(p * 100)}%`;

/** Today's date as yyyy-MM-dd in local time. */
export function todayIso(): string {
  const d = new Date();
  const mm = String(d.getMonth() + 1).padStart(2, '0');
  const dd = String(d.getDate()).padStart(2, '0');
  return `${d.getFullYear()}-${mm}-${dd}`;
}

/** NT convention used by the API: wet Nov–Apr, dry May–Oct. */
export function seasonFromDate(iso: string): 'wet' | 'dry' {
  const month = Number(iso.slice(5, 7));
  return month >= 5 && month <= 10 ? 'dry' : 'wet';
}

/** Join class names, skipping falsy values. */
export function cx(...classes: Array<string | false | null | undefined>): string {
  return classes.filter(Boolean).join(' ');
}

/** 12_345_678 → "11.8 MB" */
export function fileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(0)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

/** Time of day of an ISO timestamp in NT time (Australia/Darwin), e.g. "18:21:05". */
export function ntTime(iso: string): string {
  return new Date(iso).toLocaleTimeString('en-AU', { timeZone: 'Australia/Darwin', hour12: false });
}
