import { useState } from 'react';
import { AlertTriangle, ChevronDown } from 'lucide-react';
import type { CameraType } from '../../api/types';
import { Badge, ErrorAlert, FormField, Input, Select } from '@shared/controls';
import { AssumptionsPanel } from '../../components';
import { percent } from '@shared/lib/format';
import { GROUND_COVER, GROUND_COVER_OPTIONS, type GroundCoverKey } from './constants';
import { useEnvironmentProbability } from './hooks';

/** The study's environment model (temperature + ground cover) in a collapsible card, always labelled approximate (D-005). */
export function EnvironmentModelCard({ camera }: { camera: CameraType }) {
  const [open, setOpen] = useState(true);
  const [cover, setCover] = useState<GroundCoverKey>('open');
  const [temp, setTemp] = useState('30');
  const env = useEnvironmentProbability(camera, GROUND_COVER[cover].canopy, temp, open);
  // The approximation note is shown below; don't repeat it from the API's warnings.
  const otherAssumptions = env.data?.assumptions.filter((a) => !/canopy|approximat/i.test(a.message)) ?? [];

  return (
    <details
      id="environment"
      open={open}
      onToggle={(e) => setOpen((e.currentTarget as HTMLDetailsElement).open)}
      className="group rounded-xl border border-gray-100 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-800"
    >
      <summary className="flex cursor-pointer list-none items-center justify-between gap-3 rounded-xl p-5 focus:outline-none focus-visible:ring-focus md:px-6 [&::-webkit-details-marker]:hidden">
        <span className="flex items-center gap-2 text-base font-semibold text-gray-900 dark:text-gray-200">
          Temperature &amp; ground cover <Badge tone="warning">Approximate</Badge>
        </span>
        <ChevronDown
          className="size-5 shrink-0 text-gray-500 transition-transform group-open:rotate-180"
          aria-hidden="true"
        />
      </summary>

      <div className="space-y-4 px-5 pb-5 md:px-6 md:pb-6">
        <p className="text-sm text-gray-500 dark:text-gray-400">
          The study's second model. Tree cover was measured with GPS collars in the study; here it is approximated from
          the ground type.
        </p>
        <div className="grid grid-cols-1 items-end gap-4 sm:grid-cols-[1.4fr_1fr_auto]">
          <FormField label="Ground cover" htmlFor="cover">
            <Select id="cover" value={cover} options={GROUND_COVER_OPTIONS} onValueChange={setCover} />
          </FormField>
          {camera === 'thermal' ? (
            <FormField label="Air temperature (°C)" htmlFor="temp">
              <Input id="temp" type="number" min={0} max={50} value={temp} onChange={(e) => setTemp(e.target.value)} />
            </FormField>
          ) : (
            <span className="hidden sm:block" />
          )}
          <p className="pb-1 text-sm text-gray-700 dark:text-gray-300" aria-live="polite">
            Chance visible:{' '}
            <span className="text-2xl font-bold tabular-nums text-brand-600 dark:text-brand-300">
              {env.data ? percent(env.data.probability) : env.isError ? '—' : '…'}
            </span>
          </p>
        </div>

        <div className="flex gap-2 rounded-lg bg-gray-100 px-4 py-3 text-sm text-gray-700 dark:bg-gray-700 dark:text-gray-300">
          <AlertTriangle className="mt-0.5 size-4 shrink-0 text-gray-500" aria-hidden="true" />
          <p>
            Approximate: the model needs 'time under canopy', which the study measured with GPS collars. Here it is
            estimated from the ground cover you entered.
          </p>
        </div>

        {env.isError && <ErrorAlert error={env.error} />}
        {otherAssumptions.length > 0 && <AssumptionsPanel assumptions={otherAssumptions} />}
      </div>
    </details>
  );
}
