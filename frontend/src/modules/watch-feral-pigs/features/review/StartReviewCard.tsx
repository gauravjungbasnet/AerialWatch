import { useNavigate } from 'react-router-dom';
import { PlayCircle } from 'lucide-react';
import type { SurveyDetail } from '../../api/types';
import { Alert, Button, ButtonLink, Card, ErrorAlert } from '@shared/controls';
import { wfpPath } from '../../paths';
import { useStartReview } from './hooks';

/** Shown on the review page until the review starts: explains what starting does, then starts it. */
export function StartReviewCard({ survey: s }: { survey: SurveyDetail }) {
  const start = useStartReview(s.id);
  const navigate = useNavigate();
  const canStart = (s.status === 'uploading' || s.status === 'processing') && s.imageCount > 0;

  return (
    <Card title="Ready to review?" className="max-w-2xl">
      {canStart ? (
        <>
          <p className="mb-3 text-sm text-gray-700 dark:text-gray-300">
            You'll go through the {s.imageCount} photo{s.imageCount === 1 ? '' : 's'} one by one: accept or reject each
            AI suggestion, draw a box around any pig the AI missed, then mark the photo as checked. The confirmed count
            feeds the population estimate.
          </p>
          <Alert tone="info" className="mb-4">
            Starting the review locks the photo list: photos can't be added or removed afterwards.
          </Alert>
          <Button
            icon={<PlayCircle className="size-4" aria-hidden="true" />}
            loading={start.isPending}
            onClick={() =>
              start.mutate(undefined, {
                onSuccess: () => navigate(wfpPath(`/surveys/${s.id}/review`), { replace: true }),
              })
            }
          >
            Start review
          </Button>
          {start.isError && (
            <div className="mt-3">
              <ErrorAlert error={start.error} title="Couldn't start the review" />
            </div>
          )}
        </>
      ) : (
        <>
          <p className="mb-4 text-sm text-gray-700 dark:text-gray-300">
            Upload the flight's photos before starting the review.
          </p>
          <ButtonLink to={wfpPath(`/surveys/${s.id}`)} variant="outline">
            Go to the survey
          </ButtonLink>
        </>
      )}
    </Card>
  );
}
