import type { SurveyDetail } from '../../api/types';
import { cx, percent } from '@shared/lib/format';
import { windowLabel } from '../../domain/timeWindows';

/** Four tiles: date, time window, camera and the study's chance a pig is visible for these conditions. */
export function SurveyStatsBar({ survey: s }: { survey: SurveyDetail }) {
  return (
    <section
      aria-label="Survey conditions"
      className="grid grid-cols-2 gap-4 rounded-xl border border-gray-200 bg-white p-4 shadow-sm md:grid-cols-4 dark:border-gray-700 dark:bg-gray-800"
    >
      <Tile
        tone="bg-orange-50 dark:bg-orange-900/20"
        label="Date"
        value={s.surveyDate}
        sub={`${s.season === 'wet' ? 'Wet' : 'Dry'} season`}
      />
      <Tile
        tone="bg-blue-50 dark:bg-blue-900/20"
        label="Time window"
        value={windowLabel(s.timeWindow)}
        sub={`Start ${s.startTime}`}
      />
      <Tile
        tone="bg-green-50 dark:bg-green-900/20"
        label="Camera"
        value={s.camera === 'thermal' ? 'Thermal' : 'RGB'}
        sub={s.altitudeM ? `${s.altitudeM} m altitude` : undefined}
      />
      <div className="flex flex-col justify-center rounded-lg bg-brand-600 p-3 text-white">
        <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-brand-200">Chance visible</p>
        <p className="text-2xl font-bold tracking-tight">
          {s.detectionProbability != null ? percent(s.detectionProbability) : 'n/a'}
        </p>
      </div>
    </section>
  );
}

function Tile({ tone, label, value, sub }: { tone: string; label: string; value: string; sub?: string }) {
  return (
    <div className={cx('rounded-lg p-3', tone)}>
      <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-gray-500 dark:text-gray-400">{label}</p>
      <p className="text-base font-bold text-gray-900 dark:text-gray-100">{value}</p>
      {sub && <p className="mt-0.5 text-xs text-gray-500 dark:text-gray-400">{sub}</p>}
    </div>
  );
}
