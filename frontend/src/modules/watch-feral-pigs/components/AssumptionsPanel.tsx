import type { Assumption } from '../api/types';
import { Alert, Collapse } from '@shared/controls';

interface Props {
  assumptions: Assumption[];
  title?: string;
  /** Start with the info list expanded. */
  defaultOpen?: boolean;
}

/** Warnings always visible; the rest in a collapsible list. */
export function AssumptionsPanel({ assumptions, title = 'Assumptions', defaultOpen = false }: Props) {
  const warnings = assumptions.filter((a) => a.level === 'warning');
  const infos = assumptions.filter((a) => a.level === 'info');

  return (
    <div className="space-y-3">
      {warnings.length > 0 && (
        <Alert tone="warning">
          {warnings.length === 1 ? (
            warnings[0].message
          ) : (
            <ul className="list-disc space-y-1 pl-4">
              {warnings.map((a, i) => (
                <li key={i}>{a.message}</li>
              ))}
            </ul>
          )}
        </Alert>
      )}
      {infos.length > 0 && (
        <Collapse title={title} defaultOpen={defaultOpen}>
          <ul className="list-disc space-y-2.5 pl-4 leading-relaxed text-gray-600 marker:text-gray-300 dark:text-gray-400">
            {infos.map((a, i) => (
              <li key={i}>{a.message}</li>
            ))}
          </ul>
        </Collapse>
      )}
    </div>
  );
}
