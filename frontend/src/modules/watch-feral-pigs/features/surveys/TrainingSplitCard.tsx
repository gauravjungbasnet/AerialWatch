import { Link } from 'react-router-dom';
import { Card, ErrorAlert, Spinner } from '@shared/controls';
import { TrainingSplitPicker } from '@shared/components/training';
import type { SurveyDetail } from '../../api/types';
import { wfpPath } from '../../paths';
import { useSetTrainingSplit } from '../../api/queries/training';

/** Survey page: put this flight in the Train / Validation / Test set for AI training, or leave it out (D-038). */
export function TrainingSplitCard({ survey }: { survey: SurveyDetail }) {
  const setSplit = useSetTrainingSplit(survey.id);
  const value = setSplit.isPending ? (setSplit.variables ?? null) : (survey.trainingSplit ?? null);

  return (
    <Card
      title="AI training data"
      description="Whole flights go to one set. Only checked, unflagged photos and accepted pig boxes are exported."
      actions={
        <Link
          className="text-sm font-medium text-brand-600 hover:underline dark:text-brand-300"
          to={wfpPath('/admin/training-data')}
        >
          Open AI training data →
        </Link>
      }
    >
      <div className="flex flex-wrap items-end gap-4">
        <TrainingSplitPicker value={value} onChange={(v) => setSplit.mutate(v)} className="min-w-72 flex-1" />
        {setSplit.isPending && <Spinner label="Saving…" />}
      </div>
      {setSplit.isError && (
        <div className="mt-3">
          <ErrorAlert error={setSplit.error} title="Couldn't save the training set" />
        </div>
      )}
    </Card>
  );
}
