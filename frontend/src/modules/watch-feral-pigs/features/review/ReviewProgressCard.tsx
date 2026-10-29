import { useState } from 'react';
import { CheckCheck, Lock, Unlock } from 'lucide-react';
import type { ReviewSummary } from '../../api/types';
import { Alert, Button, Card, ErrorAlert, Modal } from '@shared/controls';
import { percent } from '@shared/lib/format';
import { useBulkAccept, useCompleteReview, useReopenReview } from './hooks';

/** Survey-level progress: photos checked, confirmed pigs, AI suggestions; bulk accept; complete / reopen the review. */
export function ReviewProgressCard({ summary: s }: { summary: ReviewSummary }) {
  const complete = useCompleteReview(s.surveyId);
  const reopen = useReopenReview(s.surveyId);
  const bulk = useBulkAccept(s.surveyId);
  const [threshold, setThreshold] = useState(0.8);
  const [confirmBulk, setConfirmBulk] = useState(false);
  const [lastBulk, setLastBulk] = useState<number | null>(null);

  const done = s.photos ? s.reviewedPhotos / s.photos : 0;
  const editable = s.status === 'inReview';

  return (
    <Card title="Review progress">
      <div className="mb-1 flex justify-between text-sm">
        <span className="text-gray-600 dark:text-gray-400">Photos checked</span>
        <span className="font-semibold text-gray-900 dark:text-gray-100">
          {s.reviewedPhotos} / {s.photos}
        </span>
      </div>
      <div
        role="progressbar"
        aria-label="Photos checked"
        aria-valuemin={0}
        aria-valuemax={s.photos}
        aria-valuenow={s.reviewedPhotos}
        className="mb-4 h-2 overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700"
      >
        <div className="h-full rounded-full bg-brand-600 transition-[width]" style={{ width: percent(done) }} />
      </div>

      <dl className="mb-5 grid grid-cols-2 gap-x-4 gap-y-5 text-sm">
        <Stat label="Confirmed pigs" value={s.confirmedPigs} strong />
        <Stat label="Photos with pigs" value={s.photosWithPigs} />
        <Stat label="AI to check" value={s.pendingSuggestions} />
        <Stat label="Added by rangers" value={s.rangerAdded} />
        <Stat label="AI rejected" value={s.rejectedSuggestions} />
        <Stat label="Flagged photos" value={s.flaggedPhotos} />
      </dl>

      {editable && s.pendingSuggestions > 0 && (
        <div className="mb-4 rounded-lg bg-gray-50 p-3 dark:bg-gray-700/50">
          <label htmlFor="bulk-threshold" className="block text-sm font-medium text-gray-700 dark:text-gray-300">
            Accept AI suggestions at least {percent(threshold)} sure
          </label>
          <input
            id="bulk-threshold"
            type="range"
            min={0.5}
            max={0.99}
            step={0.01}
            value={threshold}
            onChange={(e) => setThreshold(Number(e.target.value))}
            className="my-2 w-full accent-brand-600"
          />
          <Button
            size="sm"
            variant="outline"
            icon={<CheckCheck className="size-4" aria-hidden="true" />}
            onClick={() => setConfirmBulk(true)}
          >
            Accept all ≥ {percent(threshold)}
          </Button>
          {lastBulk !== null && (
            <p className="mt-2 text-xs text-gray-600 dark:text-gray-400">
              {lastBulk} suggestion{lastBulk === 1 ? '' : 's'} accepted.
            </p>
          )}
        </div>
      )}

      {editable ? (
        <>
          {!s.canComplete && s.blockedReason && (
            <p className="mb-3 text-sm text-gray-600 dark:text-gray-400">{s.blockedReason}</p>
          )}
          {s.canComplete && s.flaggedPhotos > 0 && (
            <Alert tone="warning" className="mb-3">
              {s.flaggedPhotos} photo{s.flaggedPhotos === 1 ? ' is' : 's are'} flagged for a second look.
            </Alert>
          )}
          <Button
            block
            disabled={!s.canComplete}
            loading={complete.isPending}
            icon={<Lock className="size-4" aria-hidden="true" />}
            onClick={() => complete.mutate()}
          >
            Complete review
          </Button>
        </>
      ) : s.status === 'reviewed' ? (
        <>
          <Alert tone="success" className="mb-3">
            Review complete: {s.confirmedPigs} confirmed pig{s.confirmedPigs === 1 ? '' : 's'}. Boxes are now read-only.
          </Alert>
          <Button
            block
            variant="outline"
            loading={reopen.isPending}
            icon={<Unlock className="size-4" aria-hidden="true" />}
            onClick={() => reopen.mutate()}
          >
            Reopen to correct
          </Button>
        </>
      ) : null}

      {(complete.isError || reopen.isError) && (
        <div className="mt-3">
          <ErrorAlert error={complete.error ?? reopen.error} title="Couldn't change the review" />
        </div>
      )}

      <Modal
        open={confirmBulk}
        onClose={() => setConfirmBulk(false)}
        title="Accept confident suggestions?"
        footer={
          <>
            <Button variant="outline" className="w-full sm:w-auto" onClick={() => setConfirmBulk(false)}>
              Cancel
            </Button>
            <Button
              className="w-full sm:w-auto"
              loading={bulk.isPending}
              onClick={() =>
                bulk.mutate(threshold, {
                  onSuccess: (r) => {
                    setLastBulk(r.accepted);
                    setConfirmBulk(false);
                  },
                })
              }
            >
              Accept
            </Button>
          </>
        }
      >
        <p>
          Every AI suggestion in this survey that is at least {percent(threshold)} sure and not yet checked will be
          accepted as a pig. Suggestions you have already rejected are not changed. You can still undo any box
          afterwards.
        </p>
        {bulk.isError && (
          <div className="mt-3">
            <ErrorAlert error={bulk.error} title="Couldn't accept" />
          </div>
        )}
      </Modal>
    </Card>
  );
}

function Stat({ label, value, strong }: { label: string; value: number; strong?: boolean }) {
  return (
    <div>
      <dt className="mb-1 text-[11px] font-medium uppercase tracking-wide text-gray-600 dark:text-gray-400">{label}</dt>
      <dd
        className={
          strong
            ? 'text-3xl font-bold text-brand-700 dark:text-brand-300'
            : 'text-xl font-semibold text-gray-900 dark:text-gray-100'
        }
      >
        {value}
      </dd>
    </div>
  );
}
