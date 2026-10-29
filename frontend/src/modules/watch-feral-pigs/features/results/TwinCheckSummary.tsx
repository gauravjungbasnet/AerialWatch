import { useTwinCheckSummary } from '../review/twinHooks';
import { cameraName } from '../../domain/cameras';

const pct = (n: number, d: number) => (d > 0 ? `${Math.round((n / d) * 100)}%` : '–');

/**
 * Thermal vs colour (D-040): of the confirmed pigs that have a twin photo, how many the ranger could also see on the
 * other camera. A live, local version of the study's finding that detection differs by camera (Hvala et al. 2023).
 */
export function TwinCheckSummary({ surveyId }: { surveyId: string }) {
  const summary = useTwinCheckSummary(surveyId);
  const s = summary.data;
  if (!s?.twinCamera) return null;
  const name = cameraName(s.twinCamera).toLowerCase();

  return (
    <section aria-label={`Seen on ${name}`} className="space-y-2 text-sm">
      <h3 className="font-semibold text-gray-900 dark:text-gray-100">Seen on the {name} photo too</h3>
      <dl className="grid grid-cols-3 gap-2 text-center">
        <div className="rounded-lg bg-gray-50 p-2 dark:bg-gray-700">
          <dt className="text-xs text-gray-500 dark:text-gray-400">Checked</dt>
          <dd className="text-lg font-bold tabular-nums">
            {s.checked} / {s.pigsWithTwin}
          </dd>
        </div>
        <div className="rounded-lg bg-gray-50 p-2 dark:bg-gray-700">
          <dt className="text-xs text-gray-500 dark:text-gray-400">Visible</dt>
          <dd className="text-lg font-bold tabular-nums">{s.visibleOnTwin}</dd>
        </div>
        <div className="rounded-lg bg-gray-50 p-2 dark:bg-gray-700">
          <dt className="text-xs text-gray-500 dark:text-gray-400">Not visible</dt>
          <dd className="text-lg font-bold tabular-nums">{s.notVisibleOnTwin}</dd>
        </div>
      </dl>
      {s.seenOnlyOnTwin > 0 && (
        <p className="text-xs text-gray-600 dark:text-gray-400">
          {s.seenOnlyOnTwin} pig{s.seenOnlyOnTwin === 1 ? ' was' : 's were'} seen only on the {name} photo (not
          counted).
        </p>
      )}
      <p className="text-xs text-gray-600 dark:text-gray-400">
        {s.checked > 0
          ? `${pct(s.visibleOnTwin, s.checked)} of checked pigs were also visible on the ${name} photo. These checks never change the count.`
          : `Check pigs on the ${name} photo in the review to compare the two cameras.`}
      </p>
    </section>
  );
}
