import { useState, type ReactNode } from 'react';
import { Download } from 'lucide-react';
import { Alert, Button, Card, ErrorAlert, Modal } from '../../controls';
import { trainingSplitLabel, type TrainingExportResult } from '../../types/training';

interface TrainingExportPanelProps {
  exportRoot: string;
  canExport: boolean;
  exporting: boolean;
  /** Usable photos that will be copied (for the confirm text). */
  photoCount: number;
  onExport: () => void;
  result?: TrainingExportResult;
  error?: unknown;
  /** Extra text in the confirm dialog, e.g. a data-handling reminder. */
  confirmNote?: ReactNode;
  /** Shown under a successful export, e.g. the next command to run. */
  nextSteps?: ReactNode;
}

/** Export card: target folder, one button with a confirm step, then the result (folder, totals, warnings). */
export function TrainingExportPanel(props: TrainingExportPanelProps) {
  const { exportRoot, canExport, exporting, photoCount, onExport, result, error, confirmNote, nextSteps } = props;
  const [confirming, setConfirming] = useState(false);

  return (
    <Card
      title="Export YOLO dataset"
      description={
        <>
          Writes a new folder under <code className="break-all">{exportRoot}</code>. Nothing is uploaded anywhere.
        </>
      }
      actions={
        <Button
          icon={<Download className="size-4" aria-hidden="true" />}
          disabled={!canExport}
          loading={exporting}
          onClick={() => setConfirming(true)}
        >
          Export dataset
        </Button>
      }
    >
      {!canExport && (
        <p className="text-sm text-gray-600 dark:text-gray-400">
          Export is available once Train, Validation and Test each have at least one checked photo.
        </p>
      )}
      {error != null && <ErrorAlert error={error} title="Export failed" />}
      {result && (
        <Alert tone="success" title="Dataset exported">
          <p className="break-all">
            <code>{result.folder}</code>
          </p>
          <ul className="mt-2 list-disc pl-5">
            {result.totals.map((t) => (
              <li key={t.split}>
                {trainingSplitLabel[t.split]}: {t.photos} photos ({t.photosWithLabels} with objects, {t.labels} boxes)
              </li>
            ))}
          </ul>
          {result.warnings.length > 0 && (
            <details className="mt-2">
              <summary className="cursor-pointer">
                {result.warnings.length} warning{result.warnings.length === 1 ? '' : 's'}
              </summary>
              <ul className="mt-1 list-disc pl-5">
                {result.warnings.map((w) => (
                  <li key={w}>{w}</li>
                ))}
              </ul>
            </details>
          )}
          {nextSteps && <div className="mt-3">{nextSteps}</div>}
        </Alert>
      )}

      <Modal
        open={confirming}
        onClose={() => setConfirming(false)}
        title="Export training dataset?"
        footer={
          <>
            <Button variant="outline" onClick={() => setConfirming(false)}>
              Cancel
            </Button>
            <Button
              onClick={() => {
                setConfirming(false);
                onExport();
              }}
            >
              Export
            </Button>
          </>
        }
      >
        <p className="text-sm text-gray-700 dark:text-gray-300">
          Copies {photoCount} photo{photoCount === 1 ? '' : 's'} and writes their labels to a new folder under{' '}
          <code className="break-all">{exportRoot}</code>.
        </p>
        {confirmNote && <div className="mt-3 text-sm text-gray-700 dark:text-gray-300">{confirmNote}</div>}
      </Modal>
    </Card>
  );
}
