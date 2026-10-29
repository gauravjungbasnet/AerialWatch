import { cx } from '../../lib/format';
import { trainingSplitLabel, type TrainingSplitTotals } from '../../types/training';

const tone: Record<string, string> = {
  train: 'bg-blue-50 dark:bg-blue-900/20',
  validation: 'bg-orange-50 dark:bg-orange-900/20',
  test: 'bg-green-50 dark:bg-green-900/20',
};

interface TrainingTotalsProps {
  totals: TrainingSplitTotals[];
  /** e.g. "Surveys" */
  sessionLabel?: string;
  /** e.g. "Pigs" */
  labelName?: string;
}

/** Three tiles (Train, Validation, Test): sessions, usable photos, photos with objects, objects. Empty sets are highlighted. */
export function TrainingTotals({ totals, sessionLabel = 'Sessions', labelName = 'Objects' }: TrainingTotalsProps) {
  return (
    <section aria-label="Totals per set" className="grid gap-4 md:grid-cols-3">
      {totals.map((t) => (
        <div
          key={t.split}
          className={cx(
            'rounded-xl border p-4 shadow-sm',
            tone[t.split],
            t.photos === 0 ? 'border-red-300 dark:border-red-700' : 'border-gray-100 dark:border-gray-700',
          )}
        >
          <p className="mb-2 text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">
            {trainingSplitLabel[t.split]}
          </p>
          <p className="text-2xl font-bold text-gray-900 dark:text-gray-100">
            {t.photos} <span className="text-sm font-medium text-gray-500 dark:text-gray-400">photos</span>
          </p>
          <dl className="mt-2 grid grid-cols-3 gap-2 text-xs text-gray-600 dark:text-gray-400">
            <div>
              <dt>{sessionLabel}</dt>
              <dd className="font-semibold text-gray-800 dark:text-gray-200">{t.sessions}</dd>
            </div>
            <div>
              <dt>With {labelName.toLowerCase()}</dt>
              <dd className="font-semibold text-gray-800 dark:text-gray-200">{t.photosWithLabels}</dd>
            </div>
            <div>
              <dt>{labelName}</dt>
              <dd className="font-semibold text-gray-800 dark:text-gray-200">{t.labels}</dd>
            </div>
          </dl>
          {t.photos === 0 && (
            <p className="mt-2 text-xs font-medium text-red-700 dark:text-red-300">
              Empty: export needs at least one photo here.
            </p>
          )}
        </div>
      ))}
    </section>
  );
}
