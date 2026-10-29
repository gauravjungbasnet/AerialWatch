import { useMemo, useState } from 'react';
import { CheckCircle2, ChevronRight, MapPin, XCircle } from 'lucide-react';
import {
  Alert,
  Badge,
  Button,
  DataGrid,
  FilterBar,
  type DataGridColumn,
  type FilterField,
  type Tone,
} from '../../../../../shared/controls';
import { applyFilters, emptyFilters, matchesText, type FilterPredicates } from '../../../../../shared/lib/filters';
import { ntTime } from '../../../../../shared/lib/format';
import { wfpApi } from '../../../api/client';
import type { UploadFileResult, UploadFileStatus } from '../../../api/types';

const RESULT: Record<UploadFileStatus, { label: string; tone: Tone; order: number }> = {
  rejected: { label: 'Not accepted', tone: 'danger', order: 0 },
  duplicate: { label: 'Already in survey', tone: 'neutral', order: 1 },
  uploaded: { label: 'Uploaded', tone: 'success', order: 2 },
};

// ---- Filters (all in the browser: the results of one upload are already loaded) ----

const presence = [
  { value: 'yes', label: 'In photo' },
  { value: 'no', label: 'Not in photo' },
];

const filterFields: FilterField[] = [
  { key: 'file', label: 'File', type: 'text', placeholder: 'Filter by file name' },
  {
    key: 'result',
    label: 'Result',
    type: 'select',
    options: (['uploaded', 'duplicate', 'rejected'] as const).map((s) => ({ value: s, label: RESULT[s].label })),
  },
  { key: 'time', label: 'Capture time', type: 'select', options: presence, placeholder: 'Any' },
  { key: 'gps', label: 'GPS', type: 'select', options: presence, placeholder: 'Any' },
];

const hasTime = (r: UploadFileResult) => Boolean(r.image?.takenAt);
const hasGps = (r: UploadFileResult) => r.image?.latitude != null && r.image?.longitude != null;

const predicates: FilterPredicates<UploadFileResult> = {
  file: (r, v) => matchesText(r.fileName, v),
  result: (r, v) => r.status === v,
  // Only uploaded photos have a time/GPS to check.
  time: (r, v) => Boolean(r.image) && hasTime(r) === (v === 'yes'),
  gps: (r, v) => Boolean(r.image) && hasGps(r) === (v === 'yes'),
};

interface Props {
  results: UploadFileResult[];
  error?: string;
  cancelled?: boolean;
  onClose: () => void;
}

/** Per-file outcome of an upload: filter bar + paged grid, problems first. */
export function UploadResultsTable({ results, error, cancelled, onClose }: Props) {
  const [filters, setFilters] = useState(() => emptyFilters(filterFields.map((f) => f.key)));
  const rows = useMemo(() => applyFilters(results, filters, predicates), [results, filters]);

  const count = (s: UploadFileStatus) => results.filter((r) => r.status === s).length;
  const rejected = count('rejected');
  const duplicates = count('duplicate');
  const title = error ? 'Upload stopped' : cancelled ? 'Upload cancelled' : 'Upload finished';
  const Icon = error || rejected ? XCircle : CheckCircle2;

  return (
    <section aria-label="Upload results" className="flex flex-col gap-3">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap items-center gap-2">
          <Icon className={error || rejected ? 'size-5 text-orange-500' : 'size-5 text-green-600'} aria-hidden="true" />
          <h4 className="font-semibold text-gray-800 dark:text-gray-200">{title}</h4>
          <Badge tone="success">{count('uploaded')} uploaded</Badge>
          {duplicates > 0 && <Badge tone="neutral">{duplicates} already in survey</Badge>}
          {rejected > 0 && <Badge tone="danger">{rejected} not accepted</Badge>}
        </div>
        <Button variant="ghost" size="sm" onClick={onClose}>
          Clear list
        </Button>
      </div>

      {error && (
        <Alert tone="danger">{error} Check the photo gallery before retrying; photos already uploaded are kept.</Alert>
      )}
      {cancelled && !error && (
        <Alert tone="info">
          Upload cancelled. Check the photo gallery before retrying; photos already uploaded are kept.
        </Alert>
      )}

      <DataGrid
        label="Upload results by file"
        toolbar={<FilterBar fields={filterFields} values={filters} onChange={setFilters} />}
        columns={columns}
        rows={rows}
        rowKey={(r, i) => `${r.fileName}-${i}`}
        defaultSort={{ field: 'status', direction: 'asc' }}
        pageSize={10}
        pageSizeOptions={[10, 20, 50]}
        maxHeight="28rem"
        empty="No files match these filters."
      />
    </section>
  );
}

// Column config: `field` is the property shown and sorted on, `header` the title, `sortable` enables header sorting.
const columns: DataGridColumn<UploadFileResult>[] = [
  { field: 'fileName', header: 'File', sortable: true, title: (r) => r.fileName, className: 'max-w-56 truncate' },
  {
    field: 'status',
    header: 'Result',
    sortable: true,
    sortValue: (r) => RESULT[r.status].order, // problems first when ascending
    cell: (r) => <Badge tone={RESULT[r.status].tone}>{RESULT[r.status].label}</Badge>,
  },
  {
    field: 'image.takenAt',
    header: 'Capture time',
    sortable: true,
    cell: (r) => (r.image ? hasTime(r) ? ntTime(r.image.takenAt!) : <Missing /> : <Dash />),
  },
  {
    field: 'image.latitude',
    header: 'GPS',
    cell: (r) =>
      !r.image ? (
        <Dash />
      ) : hasGps(r) ? (
        <span className="inline-flex items-center gap-1">
          <MapPin className="size-3" aria-hidden="true" />
          {r.image.latitude!.toFixed(4)}, {r.image.longitude!.toFixed(4)}
        </span>
      ) : (
        <Missing />
      ),
  },
  { field: 'notes', header: 'Notes', cell: (r) => notes(r) ?? <Dash />, className: 'min-w-64 whitespace-normal' },
  {
    field: 'actions',
    header: 'Actions',
    stickyRight: true, // stays visible while the table scrolls sideways
    cell: (r) =>
      r.image ? (
        <a
          href={wfpApi.surveyImageUrl(r.image.id)}
          target="_blank"
          rel="noreferrer"
          className="inline-flex items-center gap-1 rounded-md border border-gray-300 bg-white px-3 py-1.5 text-sm font-medium text-gray-800 hover:bg-gray-50 focus:outline-none focus-visible:ring-focus dark:border-gray-600 dark:bg-gray-800 dark:text-gray-200"
        >
          View <ChevronRight className="size-4" aria-hidden="true" />
        </a>
      ) : (
        <Dash />
      ),
  },
];

/** Reasons for duplicates/rejections; for uploaded photos only what the time/GPS columns don't already show. */
function notes(r: UploadFileResult): string | undefined {
  if (r.status !== 'uploaded') return r.message;
  if (r.image && !r.image.hasThumbnail) return 'No preview could be made; the full photo is shown instead.';
  if (!hasTime(r) || !hasGps(r))
    return [!hasTime(r) && 'Survey date/time used.', !hasGps(r) && "Won't appear on the map."]
      .filter(Boolean)
      .join(' ');
  return undefined;
}

const Missing = () => <span className="text-orange-600 dark:text-orange-400">Not in photo</span>;
const Dash = () => <span className="text-gray-400">—</span>;
