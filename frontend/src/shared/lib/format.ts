/** 0.7213 → "72%" */
export const percent = (p: number) => `${Math.round(p * 100)}%`;

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

/** @deprecated Moved to `@shared/lib/dates` (docs/18 step 6); kept for files not yet updated. */
export { ntTime, todayIso } from './dates';
