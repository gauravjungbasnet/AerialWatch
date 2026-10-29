/** Decorative Material Design icon; the surrounding control or text supplies its accessible name. */
export function MdiIcon({ path, className = 'size-6' }: { path: string; className?: string }) {
  return (
    <svg viewBox="0 0 24 24" className={className} fill="currentColor" aria-hidden="true" focusable="false">
      <path d={path} />
    </svg>
  );
}
