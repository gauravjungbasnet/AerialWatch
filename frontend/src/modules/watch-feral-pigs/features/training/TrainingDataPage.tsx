import { useState } from 'react';
import { Link } from 'react-router-dom';
import {
  Alert,
  Badge,
  Card,
  EmptyState,
  ErrorAlert,
  PageHeader,
  SegmentedControl,
  SkeletonRows,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHeadCell,
  TableHeader,
  TableRow,
} from '@shared/controls';
import { TrainingExportPanel, TrainingTotals, trainingSplitLabel } from '@shared/components/training';
import type { CameraType } from '../../api/types';
import { wfpPath } from '../../paths';
import { SurveyStatusBadge } from '../surveys/SurveyStatusBadge';
import { useExportTrainingData, useTrainingOverview } from './hooks';

const splitTone = { train: 'info', validation: 'warning', test: 'success' } as const;

/**
 * Admin › AI training data (D-038): surveys tagged Train/Validation/Test for one camera, totals, warnings and a one-click
 * YOLO export to a local folder. Tag surveys on their survey page.
 */
export function TrainingDataPage() {
  const [camera, setCamera] = useState<CameraType>('thermal');
  const overview = useTrainingOverview(camera);
  const exporter = useExportTrainingData();
  const data = overview.data;

  const changeCamera = (c: CameraType) => {
    setCamera(c);
    exporter.reset();
  };

  return (
    <>
      <PageHeader
        title="AI training data"
        description="Checked photos of tagged surveys become a training dataset. Whole flights go to one set, so overlapping photos never end up in both training and test."
      />

      <div className="space-y-6">
        <SegmentedControl<CameraType>
          name="training-camera"
          legend="Camera (thermal and RGB are exported separately)"
          value={camera}
          options={[
            { value: 'thermal', label: 'Thermal' },
            { value: 'rgb', label: 'RGB' },
          ]}
          onChange={changeCamera}
          className="max-w-sm"
        />

        {overview.isLoading && <SkeletonRows rows={5} />}
        {overview.isError && <ErrorAlert error={overview.error} title="Couldn't load training data" />}

        {data && (
          <>
            <TrainingTotals totals={data.totals} sessionLabel="Surveys" labelName="Pigs" />

            {data.warnings.length > 0 && (
              <Alert tone="warning" title="Check before exporting">
                <ul className="list-disc pl-5">
                  {data.warnings.map((w) => (
                    <li key={w}>{w}</li>
                  ))}
                </ul>
              </Alert>
            )}

            <Card
              title="Tagged surveys"
              description="Only checked, unflagged photos are exported. Labels = accepted pig boxes."
            >
              {data.surveys.length === 0 ? (
                <EmptyState
                  title="No surveys tagged yet"
                  description="Open a survey and choose Train, Validation or Test in its AI training data card."
                />
              ) : (
                <TableContainer>
                  <Table>
                    <TableHeader>
                      <tr>
                        <TableHeadCell>Survey</TableHeadCell>
                        <TableHeadCell>Flight</TableHeadCell>
                        <TableHeadCell>Set</TableHeadCell>
                        <TableHeadCell>Review</TableHeadCell>
                        <TableHeadCell className="text-right">Checked</TableHeadCell>
                        <TableHeadCell className="text-right">Flagged</TableHeadCell>
                        <TableHeadCell className="text-right">Exported photos</TableHeadCell>
                        <TableHeadCell className="text-right">Pigs</TableHeadCell>
                      </tr>
                    </TableHeader>
                    <TableBody>
                      {data.surveys.map((s) => (
                        <TableRow key={s.id}>
                          <TableCell>
                            <Link
                              className="font-semibold text-brand-600 hover:underline dark:text-brand-300"
                              to={wfpPath(`/surveys/${s.id}`)}
                            >
                              {s.name}
                            </Link>
                          </TableCell>
                          <TableCell>
                            {s.surveyDate} {s.startTime}
                          </TableCell>
                          <TableCell>
                            <Badge tone={splitTone[s.split]}>{trainingSplitLabel[s.split]}</Badge>
                          </TableCell>
                          <TableCell>
                            <SurveyStatusBadge status={s.status} />
                          </TableCell>
                          <TableCell className="text-right">
                            {s.checkedPhotos} / {s.photos}
                          </TableCell>
                          <TableCell className="text-right">{s.flaggedPhotos}</TableCell>
                          <TableCell className="text-right">{s.usablePhotos}</TableCell>
                          <TableCell className="text-right">{s.pigs}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </TableContainer>
              )}
            </Card>

            <TrainingExportPanel
              exportRoot={data.exportRoot}
              canExport={data.canExport}
              exporting={exporter.isPending}
              photoCount={data.totals.reduce((n, t) => n + t.photos, 0)}
              onExport={() => exporter.mutate(camera)}
              result={exporter.data}
              error={exporter.error}
              confirmNote={
                <>
                  NACAS photos must stay on this computer: don&apos;t commit, zip or upload the exported folder
                  (docs/11).
                </>
              }
              nextSteps={
                <p>
                  Next: in <code>src\ml</code> run{' '}
                  <code>python tools/evaluate.py --data &lt;folder&gt; --split test …</code> and{' '}
                  <code>python tools/train.py --data &lt;folder&gt;\data.yaml …</code> (docs/14).
                </p>
              }
            />
          </>
        )}
      </div>
    </>
  );
}
