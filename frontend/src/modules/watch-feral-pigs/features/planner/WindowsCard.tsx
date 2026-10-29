import { ArrowRight } from 'lucide-react';
import type { CameraType, PlannerWindow } from '../../api/types';
import { Card, ErrorAlert, SkeletonRows } from '@shared/controls';
import { percent } from '@shared/lib/format';
import { AssumptionsPanel, CitationBlock, TimeWindowChart } from '../../components';
import type { SeasonChoice } from './constants';
import { usePlannerWindows } from '../../api/queries/planner';

/** Chance a pig is visible in each study window, and the recommended flight time (best vs worst window). */
export function WindowsCard({
  camera,
  seasonChoice,
  date,
}: {
  camera: CameraType;
  seasonChoice: SeasonChoice;
  date: string;
}) {
  const planner = usePlannerWindows(camera, seasonChoice, date);
  const best = planner.data?.windows.find((w) => w.isBest);
  const worst = planner.data?.windows.find((w) => w.isWorst);

  return (
    <Card id="windows" title="Visibility forecast">
      <div aria-busy={planner.isLoading}>
        {planner.isLoading && <SkeletonRows rows={4} />}
        {planner.isError && <ErrorAlert error={planner.error} title="Couldn't load the planner" />}
        {planner.data && (
          <div className="space-y-6">
            <TimeWindowChart windows={planner.data.windows} />
            {best && worst && <RecommendedFlight best={best} worst={worst} summary={planner.data.recommendation} />}
            <div className="space-y-3">
              <CitationBlock compact />
              <AssumptionsPanel assumptions={planner.data.assumptions} title="Study conditions" />
            </div>
          </div>
        )}
      </div>
    </Card>
  );
}

function RecommendedFlight({ best, worst, summary }: { best: PlannerWindow; worst: PlannerWindow; summary: string }) {
  const chip = 'rounded-md bg-white/70 px-2 py-0.5 font-medium text-gray-700 dark:bg-gray-800 dark:text-gray-200';
  return (
    <section aria-labelledby="recommended-flight">
      <h4 id="recommended-flight" className="mb-2 text-sm font-semibold text-gray-900 dark:text-gray-100">
        Recommended flight
      </h4>
      <div className="rounded-lg bg-brand-50 p-4 dark:bg-gray-700">
        <p className="text-lg font-semibold text-brand-700 dark:text-brand-200">Peak time: {best.studyHours}</p>
        <p className="mt-2 flex flex-wrap items-center gap-2 text-sm text-gray-600 dark:text-gray-300">
          <ArrowRight className="size-4" aria-hidden="true" />
          <span className={chip} title={`Best window: ${best.label}`}>
            {percent(best.probability)} peak visibility
          </span>
          <span>vs</span>
          <span className={chip} title={`Worst window: ${worst.label}`}>
            {percent(worst.probability)} low
          </span>
        </p>
        <p className="sr-only">{summary}</p>
      </div>
    </section>
  );
}
