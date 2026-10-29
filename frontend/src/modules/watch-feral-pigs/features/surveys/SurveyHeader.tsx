import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { BarChart3, CalendarDays, ChevronLeft, Clock, Images, Info, MapPin, ScanSearch, Trash2 } from 'lucide-react';
import type { SurveyDetail } from '../../api/types';
import { Button, ButtonLink, ErrorAlert, Modal } from '@shared/controls';
import { wfpPath } from '../../paths';
import { SurveyStatusBadge } from './SurveyStatusBadge';
import { useDeleteSurvey } from './hooks';
import { AssumptionsPanel } from '../../components';

const longDate = (iso: string) =>
  new Date(`${iso}T00:00:00`).toLocaleDateString('en-AU', {
    weekday: 'short',
    day: 'numeric',
    month: 'short',
    year: 'numeric',
  });
const createdOn = (iso: string) =>
  new Date(iso).toLocaleDateString('en-AU', {
    day: 'numeric',
    month: 'short',
    year: 'numeric',
    timeZone: 'Australia/Darwin',
  });

/**
 * Page header for one survey: back link, "Survey: name", status badge and a meta line (site, flight date and time,
 * photo count, created date), with Delete on the right.
 */
export function SurveyHeader({ survey: s }: { survey: SurveyDetail }) {
  const navigate = useNavigate();
  const remove = useDeleteSurvey();
  const [confirming, setConfirming] = useState(false);
  const [showNotes, setShowNotes] = useState(false);

  const meta = 'inline-flex items-center gap-1.5';
  // Review entry point (D-034): start once photos are in, continue while in review, view once complete.
  const reviewLabel =
    s.status === 'inReview'
      ? 'Continue review'
      : s.status === 'reviewed'
        ? 'View review'
        : (s.status === 'uploading' || s.status === 'processing') && s.imageCount > 0
          ? 'Review photos'
          : null;
  // Results (Slice 5): available once the review has started; provisional until it is complete.
  const showResults = s.status === 'inReview' || s.status === 'reviewed';

  return (
    <header className="my-6 flex flex-wrap items-start justify-between gap-4">
      <div className="flex min-w-0 items-start gap-3">
        <Link
          to={wfpPath('/surveys')}
          aria-label="Back to surveys"
          className="mt-1 inline-flex size-9 shrink-0 items-center justify-center rounded-md text-gray-700 hover:bg-gray-200 focus:outline-none focus-visible:ring-focus dark:text-gray-300 dark:hover:bg-gray-700"
        >
          <ChevronLeft className="size-5" aria-hidden="true" />
        </Link>
        <div className="min-w-0">
          <h2 className="break-words text-2xl font-bold tracking-tight sm:text-3xl text-gray-900 dark:text-gray-100">
            <span className="font-semibold text-gray-500 dark:text-gray-400">Survey: </span>
            {s.name}
          </h2>
          <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-600 dark:text-gray-400">
            <SurveyStatusBadge status={s.status} />
            <span className={meta}>
              <MapPin className="size-4 text-gray-400" aria-hidden="true" />
              {s.siteName}
            </span>
            <span className={meta}>
              <CalendarDays className="size-4 text-gray-400" aria-hidden="true" />
              {longDate(s.surveyDate)}, {s.startTime}
            </span>
            <span className={meta}>
              <Images className="size-4 text-gray-400" aria-hidden="true" />
              {s.imageCount} photo{s.imageCount === 1 ? '' : 's'}
            </span>
            <span className={meta}>
              <Clock className="size-4 text-gray-400" aria-hidden="true" />
              Created {createdOn(s.createdAt)}
            </span>
          </div>
        </div>
      </div>

      <div className="flex flex-wrap gap-3">
        {reviewLabel && (
          <ButtonLink
            to={wfpPath(`/surveys/${s.id}/review`)}
            icon={<ScanSearch className="size-4" aria-hidden="true" />}
          >
            {reviewLabel}
          </ButtonLink>
        )}
        {showResults && (
          <ButtonLink
            to={wfpPath(`/surveys/${s.id}/results`)}
            variant={s.status === 'reviewed' ? 'primary' : 'outline'}
            icon={<BarChart3 className="size-4" aria-hidden="true" />}
          >
            Results
          </ButtonLink>
        )}
        <Button
          variant="outline"
          icon={<Info className="size-4" aria-hidden="true" />}
          onClick={() => setShowNotes(true)}
          aria-haspopup="dialog"
        >
          Notes and checks
        </Button>
        <Button
          variant="outline"
          icon={<Trash2 className="size-4" aria-hidden="true" />}
          onClick={() => setConfirming(true)}
        >
          Delete survey
        </Button>
      </div>

      <Modal
        open={showNotes}
        onClose={() => setShowNotes(false)}
        title="Notes and checks"
        footer={
          <Button variant="outline" onClick={() => setShowNotes(false)}>
            Close
          </Button>
        }
      >
        <div className="max-h-[60vh] space-y-4 overflow-y-auto">
          <section>
            <h3 className="mb-2 font-semibold text-gray-900 dark:text-gray-100">Survey notes</h3>
            <p className="whitespace-pre-wrap break-words">{s.notes || 'No notes added.'}</p>
          </section>
          <AssumptionsPanel assumptions={s.assumptions} title="Study conditions" defaultOpen />
        </div>
      </Modal>

      <Modal
        open={confirming}
        onClose={() => setConfirming(false)}
        title="Delete survey?"
        footer={
          <>
            <Button variant="outline" onClick={() => setConfirming(false)} className="w-full sm:w-auto">
              Cancel
            </Button>
            <Button
              variant="danger"
              loading={remove.isPending}
              className="w-full sm:w-auto"
              onClick={() => remove.mutate(s.id, { onSuccess: () => navigate(wfpPath('/surveys')) })}
            >
              Delete
            </Button>
          </>
        }
      >
        <p>
          “{s.name}” and its {s.imageCount} photo{s.imageCount === 1 ? '' : 's'} will be removed. This can't be undone.
        </p>
        {remove.isError && (
          <div className="mt-3">
            <ErrorAlert error={remove.error} title="Couldn't delete" />
          </div>
        )}
      </Modal>
    </header>
  );
}
