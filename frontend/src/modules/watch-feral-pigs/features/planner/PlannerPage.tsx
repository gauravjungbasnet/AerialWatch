import { useState } from 'react';
import type { CameraType, Season } from '../../api/types';
import { PageHeader } from '@shared/controls';
import { seasonFromDate, todayIso } from '@shared/lib/format';
import type { SeasonChoice } from './constants';
import { ConditionsCard } from './ConditionsCard';
import { WindowsCard } from './WindowsCard';
import { CorrectionCalculator } from './CorrectionCalculator';
import { EnvironmentModelCard } from './EnvironmentModelCard';

/**
 * Conditions in one row across the top; below, the visibility forecast (left) next to the count estimate and the
 * optional temperature & ground cover model (right). Stacks on small screens (D-032).
 */
export function PlannerPage() {
  const [date, setDate] = useState(todayIso());
  const [seasonChoice, setSeasonChoice] = useState<SeasonChoice>('auto');
  const [camera, setCamera] = useState<CameraType>('thermal');
  const season: Season = seasonChoice === 'auto' ? seasonFromDate(date) : seasonChoice;

  return (
    <>
      <PageHeader title="Survey planner" description="When to fly, and how many pigs a count really represents." />
      <div className="space-y-6">
        <ConditionsCard
          date={date}
          onDateChange={setDate}
          seasonChoice={seasonChoice}
          onSeasonChange={setSeasonChoice}
          camera={camera}
          onCameraChange={setCamera}
        />
        <div className="grid grid-cols-1 items-start gap-6 lg:grid-cols-12">
          <div className="lg:col-span-5">
            <WindowsCard camera={camera} seasonChoice={seasonChoice} date={date} />
          </div>
          <div className="space-y-6 lg:col-span-7">
            <CorrectionCalculator camera={camera} season={season} date={date} />
            <EnvironmentModelCard camera={camera} />
          </div>
        </div>
      </div>
    </>
  );
}
