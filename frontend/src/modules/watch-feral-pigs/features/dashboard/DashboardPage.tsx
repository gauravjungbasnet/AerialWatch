import { useState, type ReactNode } from 'react';
import { Plus, Search } from 'lucide-react';
import { mdiClipboardCheckOutline, mdiMagnify, mdiImageMultipleOutline, mdiClockOutline } from '@mdi/js';
import { MdiIcon } from '@shared/controls/MdiIcon';
import { wfpPath } from '../../paths';
import {
  ButtonLink,
  Card,
  EmptyState,
  ErrorAlert,
  Input,
  PageHeader,
  SkeletonRows,
  TableContainer,
  TableFooter,
} from '@shared/controls';
import { TimeWindowChart } from '../../components';
import { percent, todayIso } from '@shared/lib/format';
import { useDebouncedValue } from '@shared/lib/useDebouncedValue';
import { usePlannerWindows } from '../planner/hooks';
import { useSurveyStats, useSurveys } from '../surveys/hooks';
import { SurveyTable } from '../surveys/SurveyTable';

/** Dashboard content only; navigation and the app header remain shell-owned. */
export function DashboardPage() {
  const [search, setSearch] = useState('');
  const query = useDebouncedValue(search.trim(), 300);
  const surveys = useSurveys({ page: 1, pageSize: 5, search: query || undefined, sort: 'createdAt', dir: 'desc' });
  const stats = useSurveyStats();
  const today = usePlannerWindows('thermal', 'auto', todayIso());
  const list = surveys.data?.items ?? [];
  const total = surveys.data?.total ?? 0;
  const best = today.data?.windows.find((w) => w.isBest);

  return (
    <div className="min-w-0 space-y-7 pb-2">
      <PageHeader
        title="Dashboard"
        description="Northern Territory aerial wildlife surveys"
        actions={
          <ButtonLink to={wfpPath('/surveys/new')} className="rounded-xl" icon={<Plus size={16} aria-hidden="true" />}>
            New survey
          </ButtonLink>
        }
      />

      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        <DashboardMetric
          title="Surveys"
          value={stats.data?.surveys ?? (stats.isError ? 'Unavailable' : '…')}
          icon={<MdiIcon path={mdiClipboardCheckOutline} />}
          tone="border-orange-200 bg-orange-50 text-orange-600 dark:border-orange-800 dark:bg-orange-900/30 dark:text-orange-300"
        />
        <DashboardMetric
          title="Awaiting review"
          value={stats.data?.awaitingReview ?? (stats.isError ? 'Unavailable' : '…')}
          icon={<MdiIcon path={mdiMagnify} />}
          tone="border-sky-200 bg-sky-50 text-sky-600 dark:border-sky-800 dark:bg-sky-900/30 dark:text-sky-300"
        />
        <DashboardMetric
          title="Photos uploaded"
          value={stats.data?.photos ?? (stats.isError ? 'Unavailable' : '…')}
          icon={<MdiIcon path={mdiImageMultipleOutline} />}
          tone="border-emerald-200 bg-emerald-50 text-emerald-600 dark:border-emerald-800 dark:bg-emerald-900/30 dark:text-emerald-300"
        />
        <DashboardMetric
          title="Best time to fly today"
          value={best?.label ?? (today.isError ? 'Unavailable' : '…')}
          compact
          sub={best ? `${percent(best.probability)} visible · thermal` : undefined}
          icon={<MdiIcon path={mdiClockOutline} />}
          tone="border-teal-200 bg-teal-50 text-teal-600 dark:border-teal-800 dark:bg-teal-900/30 dark:text-teal-300"
        />
      </div>
      {stats.isError && <ErrorAlert error={stats.error} title="Couldn't load dashboard totals" />}

      <div className="grid min-w-0 items-stretch gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(0,1fr)]">
        <Card
          id="dashboard-surveys"
          className="flex flex-col overflow-hidden rounded-2xl border-gray-200 dark:border-gray-700"
          bodyClassName="flex flex-1 flex-col !p-0"
        >
          <div className="flex flex-wrap items-center justify-between gap-4 border-b border-gray-100 p-5 sm:px-6 dark:border-gray-700">
            <div>
              <h3 id="dashboard-surveys-title" className="text-base font-semibold text-gray-900 dark:text-gray-100">
                Recent surveys
              </h3>
              <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">Latest drone flights and uploaded photos</p>
            </div>
            <div className="relative w-full sm:w-60">
              <label htmlFor="dashboard-survey-search" className="sr-only">
                Filter surveys by name or site
              </label>
              <Search
                className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-gray-400"
                aria-hidden="true"
              />
              <Input
                id="dashboard-survey-search"
                type="search"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Filter surveys…"
                className="!mt-0 min-h-11 rounded-lg !bg-gray-50 pl-9 !text-xs dark:!bg-gray-900/40"
              />
            </div>
          </div>
          <div className="flex flex-1 flex-col" aria-busy={surveys.isFetching || search.trim() !== query}>
            {surveys.isError && (
              <div className="p-5">
                <ErrorAlert error={surveys.error} title="Couldn't load surveys" />
              </div>
            )}
            {surveys.isLoading && (
              <div className="p-5">
                <SkeletonRows rows={3} />
              </div>
            )}
            {surveys.data && (
              <>
                {list.length === 0 ? (
                  <div className="flex flex-1 items-center justify-center px-2 sm:px-6">
                    <EmptyState
                      title={query ? 'No matching surveys' : 'No surveys yet'}
                      description={
                        query ? 'Try a different survey name or site.' : 'Record a drone flight to get started.'
                      }
                    />
                  </div>
                ) : (
                  <TableContainer className="!rounded-none !shadow-none">
                    <SurveyTable surveys={list} />
                  </TableContainer>
                )}
                <div className="mt-auto">
                  <TableFooter>
                    <div className="flex flex-wrap items-center justify-between gap-2 px-2">
                      <span role="status">
                        Showing {list.length} of {total}
                        {query ? ' matching surveys' : ''}
                      </span>
                      <ButtonLink
                        to={wfpPath('/surveys')}
                        variant="link"
                        size="sm"
                        className="min-h-11 font-bold uppercase tracking-wide"
                      >
                        View all
                      </ButtonLink>
                    </div>
                  </TableFooter>
                </div>
              </>
            )}
          </div>
        </Card>

        <Card
          id="dashboard-visibility"
          title="Visibility today (thermal camera)"
          description="Chance a pig is visible in each study window, based on today's season and the Hvala et al. (2023) model."
          className="rounded-2xl border-gray-200 dark:border-gray-700"
          bodyClassName="!p-5 sm:!p-6"
        >
          {today.isLoading && <SkeletonRows rows={4} />}
          {today.isError && <ErrorAlert error={today.error} title="Couldn't load visibility windows" />}
          {today.data && <TimeWindowChart windows={today.data.windows} />}
        </Card>
      </div>
    </div>
  );
}

function DashboardMetric({
  title,
  value,
  icon,
  tone,
  sub,
  compact,
}: {
  title: string;
  value: ReactNode;
  icon: ReactNode;
  tone: string;
  sub?: string;
  compact?: boolean;
}) {
  return (
    <Card
      className="rounded-2xl border-gray-200 transition-shadow hover:shadow-md dark:border-gray-700"
      bodyClassName="!p-5"
    >
      <div className="flex items-center gap-4">
        <div className={`grid size-12 shrink-0 place-items-center rounded-2xl border ${tone}`}>{icon}</div>
        <div className="min-w-0">
          <p className="text-xs font-medium text-gray-500 dark:text-gray-400">{title}</p>
          <p
            className={`mt-1 font-bold leading-tight text-gray-900 dark:text-gray-100 ${compact ? 'text-lg' : 'text-2xl'}`}
          >
            {value}
          </p>
          {sub && <p className="mt-1 text-xs text-gray-500 dark:text-gray-400">{sub}</p>}
        </div>
      </div>
    </Card>
  );
}
