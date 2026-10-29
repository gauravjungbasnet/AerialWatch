import type { ResultCounts } from '../../api/types';
import { Card } from '@shared/controls';

const pct = (n: number, d: number) => (d > 0 ? `${Math.round((n / d) * 100)}%` : '–');

/** How the confirmed pigs were found: AI suggestions the ranger accepted vs boxes the ranger drew. A live AI accuracy check. */
export function AiVsRangerCard({ counts: c }: { counts: ResultCounts }) {
  const aiSuggested = c.confirmedFromAi + c.rejectedAi;
  const rows: Array<[string, number, string?]> = [
    ['AI suggestions accepted', c.confirmedFromAi],
    ['Sightings added by ranger', c.drawnByRanger],
    ['AI suggestions rejected', c.rejectedAi],
    ['AI suggestions not decided yet', c.pendingAi],
  ];

  return (
    <Card title="Review breakdown">
      <dl className="divide-y divide-gray-100 text-sm dark:divide-gray-700">
        {rows.map(([label, value]) => (
          <div key={label} className="flex items-center justify-between py-2">
            <dt className="text-gray-600 dark:text-gray-400">{label}</dt>
            <dd className="font-semibold tabular-nums text-gray-900 dark:text-gray-100">{value}</dd>
          </div>
        ))}
      </dl>
      <div className="mt-4 grid grid-cols-2 gap-3 text-center">
        <div className="rounded-lg bg-gray-50 p-3 dark:bg-gray-700">
          <p className="text-xl font-bold tabular-nums text-gray-900 dark:text-gray-100">
            {pct(c.confirmedFromAi, c.confirmed)}
          </p>
          <p className="text-xs text-gray-500 dark:text-gray-400">of confirmed pigs found by the AI</p>
        </div>
        <div className="rounded-lg bg-gray-50 p-3 dark:bg-gray-700">
          <p className="text-xl font-bold tabular-nums text-gray-900 dark:text-gray-100">
            {pct(c.confirmedFromAi, aiSuggested)}
          </p>
          <p className="text-xs text-gray-500 dark:text-gray-400">of decided AI suggestions were right</p>
        </div>
      </div>
    </Card>
  );
}
