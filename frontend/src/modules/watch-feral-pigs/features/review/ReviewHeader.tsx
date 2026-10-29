import { Link } from 'react-router-dom';
import { ChevronLeft, ChevronRight } from 'lucide-react';
import { ButtonLink } from '@shared/controls';
import type { ReviewSummary, SurveyDetail } from '../../api/types';
import { SurveyStatusBadge } from '../../components';
import { wfpPath } from '../../paths';

/** Survey name, status and progress, with a link to the results once the review is complete. */
export function ReviewHeader({ survey, summary }: { survey: SurveyDetail; summary?: ReviewSummary }) {
  return (
    <header className="my-6 flex flex-wrap items-start justify-between gap-4">
      <div className="flex min-w-0 items-start gap-3">
        <Link
          to={wfpPath(`/surveys/${survey.id}`)}
          aria-label="Back to the survey"
          className="mt-1 inline-flex size-9 shrink-0 items-center justify-center rounded-md text-gray-700 hover:bg-gray-200 focus:outline-none focus-visible:ring-focus dark:text-gray-300 dark:hover:bg-gray-700"
        >
          <ChevronLeft className="size-5" aria-hidden="true" />
        </Link>
        <div className="min-w-0">
          <h2 className="break-words text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl dark:text-gray-100">
            <span className="font-semibold text-gray-500 dark:text-gray-400">Review: </span>
            {survey.name}
          </h2>
          <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-600 dark:text-gray-400">
            <SurveyStatusBadge status={survey.status} />
            <span>{survey.siteName}</span>
            {summary && (
              <span>
                {summary.reviewedPhotos} of {summary.photos} photos checked · {summary.confirmedPigs} confirmed pig
                {summary.confirmedPigs === 1 ? '' : 's'}
              </span>
            )}
          </div>
        </div>
      </div>
      {summary?.status === 'reviewed' && (
        <ButtonLink
          to={wfpPath(`/surveys/${survey.id}/results`)}
          icon={<ChevronRight className="size-4" aria-hidden="true" />}
        >
          View results
        </ButtonLink>
      )}
    </header>
  );
}
