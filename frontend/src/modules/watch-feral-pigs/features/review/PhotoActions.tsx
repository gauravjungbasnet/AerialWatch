import { Flag, SquareCheck, Undo2 } from 'lucide-react';
import { Button, Kbd } from '@shared/controls';
import { cx } from '@shared/lib/format';
import type { ReviewImage } from '../../api/types';

interface PhotoActionsProps {
  photo: ReviewImage;
  editable: boolean;
  hasConfirmedPigs: boolean;
  canNext: boolean;
  completing: boolean;
  reopening: boolean;
  onCheckAndNext(): void;
  onReopen(): void;
  onNext(): void;
  onToggleFlag(): void;
}

/** Under the photo: mark it checked (or not), flag it, and its GPS position. */
export function PhotoActions({
  photo,
  editable,
  hasConfirmedPigs,
  canNext,
  completing,
  reopening,
  onCheckAndNext,
  onReopen,
  onNext,
  onToggleFlag,
}: PhotoActionsProps) {
  return (
    <div className="flex flex-wrap items-center gap-3 border-t border-gray-200 p-4 dark:border-gray-700">
      {photo.reviewed ? (
        <>
          <span className="inline-flex items-center gap-1.5 text-sm text-green-700 dark:text-green-300">
            <SquareCheck className="size-4" aria-hidden="true" />
            Checked{photo.reviewedBy ? ` by ${photo.reviewedBy}` : ''}
          </span>
          {editable && (
            <Button
              variant="outline"
              size="sm"
              className="min-h-10"
              icon={<Undo2 className="size-4" aria-hidden="true" />}
              loading={reopening}
              onClick={onReopen}
            >
              Mark not checked
            </Button>
          )}
          <Button variant="ghost" size="sm" className="min-h-10" onClick={onNext} disabled={!canNext}>
            Next photo
          </Button>
        </>
      ) : (
        editable && (
          <Button
            icon={<SquareCheck className="size-4" aria-hidden="true" />}
            loading={completing}
            onClick={onCheckAndNext}
          >
            {hasConfirmedPigs ? 'Photo checked, next' : 'No (more) pigs, next'} <Kbd>Space</Kbd>
          </Button>
        )
      )}
      {editable && (
        <Button
          variant="outline"
          size="sm"
          className={cx('min-h-10', photo.flagged && 'border-red-400! text-red-700! dark:text-red-300!')}
          aria-pressed={photo.flagged}
          icon={<Flag className="size-4" aria-hidden="true" />}
          onClick={onToggleFlag}
        >
          {photo.flagged ? 'Flagged' : 'Flag (F)'}
        </Button>
      )}
      <span className="ml-auto rounded-md border border-gray-100 bg-gray-50 px-3 py-2 font-mono text-xs text-gray-600 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-400">
        {photo.latitude != null && photo.longitude != null
          ? `GPS ${photo.latitude.toFixed(5)}, ${photo.longitude.toFixed(5)}`
          : 'No GPS'}
      </span>
    </div>
  );
}
