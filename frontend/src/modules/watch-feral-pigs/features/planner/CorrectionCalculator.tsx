import { useEffect, useState, type FormEvent } from 'react';
import { ArrowRight } from 'lucide-react';
import type { CameraType, EstimateResponse, Season } from '../../api/types';
import { Button, Card, Collapse, ErrorAlert, FormField, Input } from '@shared/controls';
import { AssumptionsPanel } from '../../components';
import { percent } from '@shared/lib/format';
import { useEstimatePreview } from './hooks';

/** Confirmed count + start time → estimated pigs present, with a likely range (never a bare number). */
export function CorrectionCalculator({ camera, season, date }: { camera: CameraType; season: Season; date: string }) {
  const [count, setCount] = useState('15');
  const [time, setTime] = useState('18:15');
  const [area, setArea] = useState('');
  const estimate = useEstimatePreview();
  const { reset } = estimate;

  // A result only stays on screen while it matches the inputs: any change hides it until Calculate is pressed again.
  useEffect(() => reset(), [camera, season, date, count, time, area, reset]);

  const onSubmit = (e: FormEvent) => {
    e.preventDefault();
    estimate.mutate({
      confirmedCount: Number(count),
      camera,
      surveyDate: date,
      startTime: time,
      season,
      areaKm2: area ? Number(area) : undefined,
    });
  };

  return (
    <Card id="correction" title="Estimate from a count">
      <form onSubmit={onSubmit} className="grid grid-cols-1 items-end gap-3 sm:grid-cols-4">
        <FormField label="Pigs counted" htmlFor="count">
          <Input
            id="count"
            type="number"
            min={0}
            step={1}
            value={count}
            onChange={(e) => setCount(e.target.value)}
            required
          />
        </FormField>
        <FormField label="Start time" htmlFor="start-time">
          <Input id="start-time" type="time" value={time} onChange={(e) => setTime(e.target.value)} required />
        </FormField>
        <FormField label="Area (km²)" htmlFor="area">
          <Input
            id="area"
            type="number"
            min={0}
            step="0.1"
            placeholder="Optional"
            value={area}
            onChange={(e) => setArea(e.target.value)}
          />
        </FormField>
        <Button type="submit" loading={estimate.isPending} block>
          Calculate
        </Button>
      </form>

      {(estimate.isError || estimate.data) && (
        <div className="mt-5">
          {estimate.isError && <ErrorAlert error={estimate.error} title="Couldn't calculate the estimate" />}
          {estimate.data && <Results result={estimate.data} />}
        </div>
      )}
    </Card>
  );
}

/** "15 counted → Estimated actual: ≈ 36 (likely 27–44) · density". Shown only after Calculate. */
function Results({ result }: { result: EstimateResponse }) {
  return (
    <div className="space-y-4" aria-live="polite">
      <div>
        <h4 className="mb-2 text-sm font-medium text-gray-700 dark:text-gray-300">Results</h4>
        <div className="grid grid-cols-[auto_auto_1fr] items-center gap-x-4 gap-y-3 sm:grid-cols-[auto_auto_1fr_auto]">
          <div>
            <p className="text-3xl font-semibold tabular-nums text-gray-900 dark:text-white">{result.confirmedCount}</p>
            <p className="text-sm text-gray-500 dark:text-gray-400">counted</p>
          </div>
          <ArrowRight className="size-5 text-gray-400" aria-hidden="true" />
          <div className="min-w-0">
            <p className="text-2xl font-semibold text-gray-900 dark:text-white">
              Estimated actual:{' '}
              <span className="tabular-nums text-brand-600 dark:text-brand-300">≈ {result.estimateRounded}</span>
            </p>
            <p className="text-sm text-gray-500 dark:text-gray-400">
              Likely {result.low}–{result.high} · {percent(result.detectionProbability)} visible
            </p>
          </div>
          <div
            className="col-span-3 sm:col-span-1 sm:text-right"
            title={result.densityPerKm2 == null ? 'Enter the area to get a density' : undefined}
          >
            <p className="text-2xl font-semibold tabular-nums text-brand-600 dark:text-brand-300">
              {result.densityPerKm2 ?? '--'}
            </p>
            <p className="text-sm text-gray-500 dark:text-gray-400">Density: pigs/km²</p>
          </div>
        </div>
      </div>
      <Collapse title="How this is calculated">
        <p className="text-gray-600 dark:text-gray-400">{result.method}</p>
        <p className="mt-2 text-xs text-gray-500">Model {result.modelVersion}</p>
      </Collapse>
      <AssumptionsPanel assumptions={result.assumptions} />
    </div>
  );
}
