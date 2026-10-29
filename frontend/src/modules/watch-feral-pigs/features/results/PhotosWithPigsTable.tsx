import type { PhotoPoint } from '../../api/types';
import { Badge, Button, Card, DataGrid, EmptyState, type DataGridColumn } from '@shared/controls';
import { ntTime } from '@shared/lib/format';

const columns: DataGridColumn<PhotoPoint>[] = [
  { field: 'fileName', header: 'Photo', sortable: true, className: 'max-w-48 truncate', title: (p) => p.fileName },
  { field: 'takenAt', header: 'Time (NT)', cell: (p) => (p.takenAt ? ntTime(p.takenAt) : '—') },
  { field: 'confirmed', header: 'Sightings', sortable: true, align: 'right' },
  {
    field: 'reviewed',
    header: 'Review',
    cell: (p) => (
      <>
        <Badge tone={p.reviewed ? 'success' : 'warning'}>{p.reviewed ? 'Checked' : 'Unchecked'}</Badge>
        {p.flagged && (
          <Badge tone="danger" className="ml-1">
            Flagged
          </Badge>
        )}
      </>
    ),
  },
];

/** Supporting photo evidence, paginated to avoid another vertical scroll area. */
export function PhotosWithPigsTable({
  photos,
  onLocate,
  onShow,
  selectedPhoto,
  onlyPhoto,
  bare = false,
}: {
  photos: PhotoPoint[];
  onLocate?: (id: string) => void;
  /** Opens the photo with its marked pigs; used for photos without GPS. */
  onShow?: (id: string) => void;
  selectedPhoto?: string;
  onlyPhoto?: string;
  bare?: boolean;
}) {
  const rows = photos.filter((p) => (onlyPhoto ? p.imageId === onlyPhoto : p.confirmed > 0));
  const photoColumns = columns.map((column) =>
    column.field !== 'fileName' || !onLocate
      ? column
      : {
          ...column,
          cell: (p: PhotoPoint) =>
            p.latitude != null && p.longitude != null ? (
              <Button
                variant="link"
                className="min-h-11 max-w-48"
                onClick={() => onLocate(p.imageId)}
                aria-label={`Show ${p.fileName} and locate it on the map`}
                aria-pressed={selectedPhoto === p.imageId}
              >
                <span className="truncate">{p.fileName}</span>
              </Button>
            ) : onShow ? (
              <Button
                variant="link"
                className="min-h-11 max-w-48"
                onClick={() => onShow(p.imageId)}
                aria-label={`Show ${p.fileName} (no GPS)`}
              >
                <span className="truncate">{p.fileName}</span>
                <span className="ml-1 shrink-0 text-xs text-gray-500 dark:text-gray-400">No GPS</span>
              </Button>
            ) : (
              <span title={p.fileName} className="block max-w-48">
                <span className="block truncate">{p.fileName}</span>
                <span className="block text-xs text-gray-500 dark:text-gray-400">No GPS</span>
              </span>
            ),
        },
  );
  const grid = (
    <DataGrid
      label={onlyPhoto ? 'Selected photo' : 'Photos with confirmed sightings'}
      columns={photoColumns}
      rows={rows}
      rowKey={(p) => p.imageId}
      defaultSort={{ field: 'confirmed', direction: 'desc' }}
      pageSize={3}
      pageSizeOptions={[]}
      maxHeight="none"
      empty={<EmptyState title="No confirmed sightings" description="Check photos in Review to confirm sightings." />}
    />
  );
  if (bare) return grid;
  return (
    <Card
      title="Photos with pigs"
      description={`${rows.length} / ${photos.length} photos · Select a linked photo to locate it.`}
      bodyClassName="p-4! sm:p-5!"
      className="shadow-lg"
    >
      {grid}
    </Card>
  );
}
