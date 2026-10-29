export const CITATION =
  'Hvala A., Rogers R.M., Alazab M., Campbell H.A. (2023). Supplementing aerial drone surveys with biotelemetry data validates wildlife detection probabilities. Frontiers in Conservation Science, 4, 1203736.';
export const DOI_URL = 'https://doi.org/10.3389/fcosc.2023.1203736';

/** Research credit. `compact` is a one-line source note. */
export function CitationBlock({ compact = false }: { compact?: boolean }) {
  if (compact) {
    return (
      <p className="text-xs text-gray-500 dark:text-gray-400">
        Source:{' '}
        <a
          className="font-medium text-brand-600 hover:underline dark:text-brand-300"
          href={DOI_URL}
          target="_blank"
          rel="noreferrer"
        >
          Hvala et al. (2023), Charles Darwin University
        </a>
      </p>
    );
  }
  return (
    <p className="rounded-lg border-l-4 border-brand-600 bg-gray-50 p-4 text-sm text-gray-700 dark:bg-gray-700 dark:text-gray-300">
      {CITATION}{' '}
      <a
        className="break-all font-medium text-brand-600 hover:underline dark:text-brand-300"
        href={DOI_URL}
        target="_blank"
        rel="noreferrer"
      >
        {DOI_URL}
      </a>
    </p>
  );
}
