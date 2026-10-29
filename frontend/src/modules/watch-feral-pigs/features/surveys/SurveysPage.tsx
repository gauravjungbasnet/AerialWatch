import { wfpPath } from '../../paths';
import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { ChevronRight, Trash2 } from 'lucide-react';
import type { CameraType, SurveyListQuery, SurveySortField, SurveyStatus, SurveySummary } from '../../api/types';
import {
  Button,
  ButtonLink,
  DataGrid,
  ErrorAlert,
  FilterBar,
  Modal,
  PageHeader,
  type DataGridColumn,
  type FilterField,
} from '@shared/controls';
import { emptyFilters, hasActiveFilters } from '@shared/lib/filters';
import type { SortState } from '@shared/lib/sorting';
import { useDebouncedValue } from '@shared/lib/useDebouncedValue';
import { SurveyStatusBadge } from './SurveyStatusBadge';
import { windowLabel } from './labels';
import { useDeleteSurvey, useSurveys } from './hooks';

// Server-side list: filters, sort and page go to GET /api/wfp/surveys/list and SQL Server does the work.

const filterFields: FilterField[] = [
  { key: 'search', label: 'Survey', type: 'text', placeholder: 'Name or site' },
  { key: 'date', label: 'Date', type: 'date' },
  {
    key: 'camera',
    label: 'Camera',
    type: 'select',
    options: [
      { value: 'thermal', label: 'Thermal' },
      { value: 'rgb', label: 'RGB' },
    ],
  },
  {
    key: 'status',
    label: 'Status',
    type: 'select',
    options: [
      { value: 'draft', label: 'Draft' },
      { value: 'uploading', label: 'Uploading' },
      { value: 'processing', label: 'AI running' },
      { value: 'inReview', label: 'In review' },
      { value: 'reviewed', label: 'Reviewed' },
      { value: 'failed', label: 'Failed' },
    ],
  },
];

export function SurveysPage() {
  const remove = useDeleteSurvey();
  const [toDelete, setToDelete] = useState<SurveySummary | null>(null);

  const [filters, setFilters] = useState(() => emptyFilters(filterFields.map((f) => f.key)));
  const [sort, setSort] = useState<SortState>(null); // null = newest first (server default)
  const [page, setPage] = useState(1);
  const [pageSize, setPageSize] = useState(20);

  // Typing waits 300 ms before a request; selects and dates feel instant enough with the same delay.
  const debounced = useDebouncedValue(filters, 300);
  const query = useMemo<SurveyListQuery>(
    () => ({
      search: debounced.search || undefined,
      date: debounced.date || undefined,
      camera: (debounced.camera || undefined) as CameraType | undefined,
      status: (debounced.status || undefined) as SurveyStatus | undefined,
      sort: sort?.field as SurveySortField | undefined,
      dir: sort?.direction,
      page,
      pageSize,
    }),
    [debounced, sort, page, pageSize],
  );
  const surveys = useSurveys(query);

  const onFilters = (next: typeof filters) => {
    setFilters(next);
    setPage(1); // new filters start from the first page
  };

  const confirmDelete = () => {
    if (!toDelete) return;
    remove.mutate(toDelete.id, { onSuccess: () => setToDelete(null) });
  };

  const columns: DataGridColumn<SurveySummary>[] = [
    {
      field: 'name',
      header: 'Survey',
      sortable: true,
      cell: (s) => (
        <Link
          to={wfpPath(`/surveys/${s.id}`)}
          className="font-medium text-gray-900 hover:text-brand-600 dark:text-gray-100"
        >
          {s.name}
        </Link>
      ),
    },
    { field: 'siteName', header: 'Site', sortable: true },
    { field: 'surveyDate', header: 'Date', sortable: true, cell: (s) => `${s.surveyDate} ${s.startTime}` },
    { field: 'timeWindow', header: 'Window', cell: (s) => windowLabel(s.timeWindow) },
    { field: 'camera', header: 'Camera', cell: (s) => (s.camera === 'thermal' ? 'Thermal' : 'RGB') },
    { field: 'imageCount', header: 'Photos', sortable: true, align: 'right', className: 'tabular-nums' },
    { field: 'status', header: 'Status', sortable: true, cell: (s) => <SurveyStatusBadge status={s.status} /> },
    {
      field: 'actions',
      header: 'Actions',
      stickyRight: true,
      cell: (s) => (
        <div className="flex items-center justify-center gap-1">
          <Link
            to={wfpPath(`/surveys/${s.id}`)}
            className="inline-flex items-center gap-1 rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-800 hover:bg-gray-50 focus:outline-none focus-visible:ring-focus dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200"
          >
            View <ChevronRight className="size-4" aria-hidden="true" />
          </Link>
          <button
            type="button"
            onClick={() => setToDelete(s)}
            aria-label={`Delete ${s.name}`}
            className="rounded-md p-2 text-gray-500 hover:bg-gray-100 hover:text-red-600 focus:outline-none focus-visible:ring-focus dark:text-gray-400 dark:hover:bg-gray-700"
          >
            <Trash2 className="size-4" aria-hidden="true" />
          </button>
        </div>
      ),
    },
  ];

  const data = surveys.data;
  const filtered = hasActiveFilters(debounced);

  return (
    <>
      <PageHeader title="Surveys" actions={<ButtonLink to={wfpPath('/surveys/new')}>New survey</ButtonLink>} />

      {surveys.isError && (
        <div className="mb-4">
          <ErrorAlert error={surveys.error} title="Couldn't load surveys" />
        </div>
      )}

      <DataGrid
        label="Surveys"
        toolbar={<FilterBar fields={filterFields} values={filters} onChange={onFilters} />}
        columns={columns}
        rows={data?.items ?? []}
        rowKey={(s) => s.id}
        loading={surveys.isFetching}
        sorting={{
          sort,
          onSortChange: (next) => {
            setSort(next);
            setPage(1);
          },
        }}
        paging={{
          page,
          pageSize,
          total: data?.total ?? 0,
          onPageChange: setPage,
          onPageSizeChange: (size) => {
            setPageSize(size);
            setPage(1);
          },
        }}
        empty={
          filtered ? (
            'No surveys match these filters.'
          ) : (
            <span>
              No surveys yet. Each survey is one drone flight.{' '}
              <Link
                to={wfpPath('/surveys/new')}
                className="font-medium text-brand-600 hover:underline dark:text-brand-300"
              >
                Create the first one
              </Link>
              .
            </span>
          )
        }
      />

      <Modal
        open={toDelete !== null}
        onClose={() => setToDelete(null)}
        title="Delete survey?"
        footer={
          <>
            <Button variant="outline" onClick={() => setToDelete(null)} className="w-full sm:w-auto">
              Cancel
            </Button>
            <Button variant="danger" loading={remove.isPending} onClick={confirmDelete} className="w-full sm:w-auto">
              Delete
            </Button>
          </>
        }
      >
        <p>“{toDelete?.name}” and its photos will be removed. This can't be undone.</p>
        {remove.isError && (
          <div className="mt-3">
            <ErrorAlert error={remove.error} title="Couldn't delete" />
          </div>
        )}
      </Modal>
    </>
  );
}
