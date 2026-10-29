import { useEffect, useState } from 'react';
import type { Detection, ReviewImage, SurveyStatus } from '../../api/types';
import type { useDrawingHistory } from './hooks';
import { useImageReview } from './hooks';

interface ReviewCommandsInput {
  surveyId: string;
  photoId: string | undefined;
  photo: ReviewImage | undefined;
  status: SurveyStatus | undefined;
  boxes: Detection[];
  selected: Detection | null;
  select(id: string): void;
  drawing: ReturnType<typeof useDrawingHistory>;
  /** Called after the photo is marked checked, to open the next one. */
  onChecked(): void;
}

/**
 * What a ranger can do to the current photo (D-034): accept / reject / undo AI suggestions, delete own boxes, mark the
 * photo checked, flag it. Edits are allowed only while the survey is in review and no other write is running.
 */
export function useReviewCommands({
  surveyId,
  photoId,
  photo,
  status,
  boxes,
  selected,
  select,
  drawing,
  onChecked,
}: ReviewCommandsInput) {
  const actions = useImageReview(surveyId, photoId);
  const [notice, setNotice] = useState<string | null>(null);
  useEffect(() => setNotice(null), [photoId]);

  const editable =
    status === 'inReview' &&
    !drawing.isPending &&
    !actions.update.isPending &&
    !actions.remove.isPending &&
    !actions.complete.isPending &&
    !actions.reopen.isPending;
  const pending = boxes.filter((b) => b.status === 'pending');
  const setStatus = (id: string, s: Detection['status']) => actions.update.mutate({ id, body: { status: s } });

  return {
    actions,
    editable,
    notice,
    setNotice,
    pending,
    setStatus,
    /** Accept or reject the selected suggestion (or the first one to check), then select the next one to check. */
    decide(s: Detection['status']) {
      const target = selected ?? pending[0];
      if (!target || target.source === 'ranger') return;
      setStatus(target.id, s);
      const next = pending.find((b) => b.id !== target.id);
      if (next) select(next.id);
    },
    acceptAll: () => pending.forEach((b) => setStatus(b.id, 'accepted')),
    /** U: an AI decision goes back to "to check". Ranger boxes have no decision to undo. */
    undoDecision: () => selected?.source === 'ai' && setStatus(selected.id, 'pending'),
    /** Del: a ranger's own box is deleted; an AI suggestion is rejected, never deleted (D-034). */
    deleteSelected() {
      if (!selected) return;
      if (selected.source === 'ranger') actions.remove.mutate(selected.id);
      else setStatus(selected.id, 'rejected');
    },
    /** Space: mark the photo checked and move on; refused while suggestions are still to check. */
    checkAndNext() {
      if (!photoId || !editable) return;
      if (pending.length > 0) {
        setNotice(
          `Accept or reject the ${pending.length} box${pending.length === 1 ? '' : 'es'} still to check first (A / R).`,
        );
        return;
      }
      actions.complete.mutate(undefined, { onSuccess: onChecked });
    },
    toggleFlag: () => photo && actions.flag.mutate(!photo.flagged),
  };
}
