import {
  Badge,
  SectionTitle,
  SkeletonRows,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHeadCell,
  TableHeader,
  TableRow,
} from '@shared/controls';
import { ntDateTime } from '@shared/lib/dates';
import type { ModelParametersHistoryItem } from '../../api/types';

/** Every saved version of the coefficients, newest first; the first one is in use. Times are NT time. */
export function SavedVersionsTable({ items, loading }: { items?: ModelParametersHistoryItem[]; loading: boolean }) {
  return (
    <section aria-labelledby="history-title">
      <SectionTitle>
        <span id="history-title">Saved versions</span>
      </SectionTitle>
      {loading && <SkeletonRows rows={3} />}
      {items && (
        <TableContainer>
          <Table>
            <TableHeader>
              <tr>
                <TableHeadCell>Version</TableHeadCell>
                <TableHeadCell>Saved (NT)</TableHeadCell>
                <TableHeadCell>By</TableHeadCell>
                <TableHeadCell className="text-right">Values</TableHeadCell>
              </tr>
            </TableHeader>
            <TableBody>
              {items.map((item, i) => (
                <TableRow key={item.id}>
                  <TableCell className="font-semibold text-gray-700 dark:text-gray-200">
                    <span className="inline-flex items-center gap-2">
                      {item.version}
                      {i === 0 && <Badge tone="success">In use</Badge>}
                      {item.isPublished && <Badge>Published</Badge>}
                    </span>
                  </TableCell>
                  <TableCell>{ntDateTime(item.savedAt)}</TableCell>
                  <TableCell>{item.savedBy ?? '—'}</TableCell>
                  <TableCell className="text-right tabular-nums">{item.values.length}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </section>
  );
}
