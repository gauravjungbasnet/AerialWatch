import { wfpPath } from '../../paths';
import { Link } from 'react-router-dom';
import type { SurveySummary } from '../../api/types';
import { windowLabel } from './labels';
import { Table, TableBody, TableCell, TableHeadCell, TableHeader, TableRow } from '@shared/controls';
import { SurveyStatusBadge } from './SurveyStatusBadge';
import type { ReactNode } from 'react';

/** Shared survey table (dashboard + surveys page). `actions` renders the last column. */
export function SurveyTable({
  surveys,
  actions,
}: {
  surveys: SurveySummary[];
  actions?: (s: SurveySummary) => ReactNode;
}) {
  return (
    <Table>
      <TableHeader>
        <tr>
          <TableHeadCell>Survey</TableHeadCell>
          <TableHeadCell>Date</TableHeadCell>
          <TableHeadCell>Window</TableHeadCell>
          <TableHeadCell>Camera</TableHeadCell>
          <TableHeadCell>Photos</TableHeadCell>
          <TableHeadCell>Status</TableHeadCell>
          {actions && (
            <TableHeadCell>
              <span className="sr-only">Actions</span>
            </TableHeadCell>
          )}
        </tr>
      </TableHeader>
      <TableBody>
        {surveys.map((s) => (
          <TableRow key={s.id}>
            <TableCell>
              <Link
                to={wfpPath(`/surveys/${s.id}`)}
                className="font-semibold text-gray-700 hover:text-brand-600 dark:text-gray-200"
              >
                {s.name}
              </Link>
              <p className="text-xs text-gray-600 dark:text-gray-400">{s.siteName}</p>
            </TableCell>
            <TableCell>
              {s.surveyDate}
              <span className="ml-2 text-xs text-gray-500">{s.startTime}</span>
            </TableCell>
            <TableCell>{windowLabel(s.timeWindow)}</TableCell>
            <TableCell>{s.camera === 'thermal' ? 'Thermal' : 'RGB'}</TableCell>
            <TableCell className="tabular-nums">{s.imageCount}</TableCell>
            <TableCell>
              <SurveyStatusBadge status={s.status} />
            </TableCell>
            {actions && <TableCell className="text-right">{actions(s)}</TableCell>}
          </TableRow>
        ))}
      </TableBody>
    </Table>
  );
}
