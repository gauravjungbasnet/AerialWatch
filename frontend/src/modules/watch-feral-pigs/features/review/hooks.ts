import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useRef } from 'react';
import { DrawingHistory } from './drawingHistory';
import { wfpApi } from '../../api/client';
import type { BoxRect, Detection, ReviewImageDetail, ReviewImageFilter, UpdateDetectionRequest } from '../../api/types';
import { reviewKeys, surveyKeys } from '../../api/keys';

export function useReviewSummary(surveyId: string) {
  return useQuery({
    queryKey: reviewKeys.summary(surveyId),
    queryFn: () => wfpApi.reviewSummary(surveyId),
    enabled: Boolean(surveyId),
  });
}

/** The queue for one filter. Keeps the previous list on screen while a new filter loads. */
export function useReviewImages(surveyId: string, filter: ReviewImageFilter) {
  return useQuery({
    queryKey: reviewKeys.images(surveyId, filter),
    queryFn: () => wfpApi.reviewImages(surveyId, filter),
    enabled: Boolean(surveyId),
    placeholderData: keepPreviousData,
  });
}

export function useReviewImage(surveyId: string, imageId: string | undefined) {
  return useQuery({
    queryKey: reviewKeys.image(surveyId, imageId ?? ''),
    queryFn: () => wfpApi.reviewImage(imageId!),
    enabled: Boolean(imageId),
  });
}

/** Refreshes the survey-level views (summary, queue, survey status) after any review change. */
function useRefreshReview(surveyId: string) {
  const qc = useQueryClient();
  return () =>
    Promise.all([
      qc.invalidateQueries({ queryKey: reviewKeys.summary(surveyId) }),
      qc.invalidateQueries({ queryKey: reviewKeys.queues(surveyId) }),
      qc.invalidateQueries({ queryKey: surveyKeys.detail(surveyId), exact: true }),
      qc.invalidateQueries({ queryKey: surveyKeys.lists }),
      qc.invalidateQueries({ queryKey: surveyKeys.stats }),
    ]);
}

export function useStartReview(surveyId: string) {
  const refresh = useRefreshReview(surveyId);
  return useMutation({ mutationFn: () => wfpApi.startReview(surveyId), onSuccess: refresh });
}

export function useCompleteReview(surveyId: string) {
  const refresh = useRefreshReview(surveyId);
  return useMutation({ mutationFn: () => wfpApi.completeReview(surveyId), onSuccess: refresh });
}

export function useReopenReview(surveyId: string) {
  const refresh = useRefreshReview(surveyId);
  return useMutation({ mutationFn: () => wfpApi.reopenReview(surveyId), onSuccess: refresh });
}

export function useBulkAccept(surveyId: string) {
  const qc = useQueryClient();
  const refresh = useRefreshReview(surveyId);
  return useMutation({
    mutationFn: (minConfidence: number) => wfpApi.bulkAccept(surveyId, minConfidence),
    onSuccess: () => Promise.all([refresh(), qc.invalidateQueries({ queryKey: reviewKeys.photos(surveyId) })]),
  });
}

/**
 * Box and photo changes for the photo on screen. Box edits update the cached photo immediately (so accept/reject
 * and drags feel instant) and roll back if the API refuses them.
 */
export function useImageReview(surveyId: string, imageId: string | undefined) {
  const qc = useQueryClient();
  const refresh = useRefreshReview(surveyId);
  const key = reviewKeys.image(surveyId, imageId ?? '');

  const patchCache = (change: (d: ReviewImageDetail) => ReviewImageDetail) => {
    const previous = qc.getQueryData<ReviewImageDetail>(key);
    if (previous) qc.setQueryData(key, change(previous));
    return previous;
  };
  const restore = (previous: ReviewImageDetail | undefined) => previous && qc.setQueryData(key, previous);
  const settle = () => Promise.all([qc.invalidateQueries({ queryKey: key }), refresh()]);

  const update = useMutation({
    mutationFn: ({ id, body }: { id: string; body: UpdateDetectionRequest }) => wfpApi.updateDetection(id, body),
    onMutate: async ({ id, body }) => {
      await qc.cancelQueries({ queryKey: key });
      return patchCache((d) => ({
        ...d,
        detections: d.detections.map((x) => (x.id === id ? ({ ...x, ...body } as Detection) : x)),
      }));
    },
    onError: (_e, _v, previous) => restore(previous),
    onSettled: settle,
  });

  const add = useMutation({
    mutationFn: (box: BoxRect) => wfpApi.addDetection(imageId!, box),
    onSuccess: (created) => patchCache((d) => ({ ...d, detections: [...d.detections, created] })),
    onSettled: settle,
  });

  const remove = useMutation({
    mutationFn: (id: string) => wfpApi.deleteDetection(id),
    onMutate: async (id) => {
      await qc.cancelQueries({ queryKey: key });
      return patchCache((d) => ({ ...d, detections: d.detections.filter((x) => x.id !== id) }));
    },
    onError: (_e, _v, previous) => restore(previous),
    onSettled: settle,
  });

  const complete = useMutation({ mutationFn: () => wfpApi.completeImage(imageId!), onSettled: settle });
  const reopen = useMutation({ mutationFn: () => wfpApi.reopenImage(imageId!), onSettled: settle });
  const flag = useMutation({
    mutationFn: (flagged: boolean) => wfpApi.flagImage(imageId!, flagged),
    onSettled: settle,
  });

  return { update, add, remove, complete, reopen, flag };
}

/** Undo/redo only newly drawn ranger boxes; each photo has its own session history. */
export function useDrawingHistory(surveyId: string, imageId: string | undefined, boxes: Detection[]) {
  const qc = useQueryClient();
  const refresh = useRefreshReview(surveyId);
  const history = useRef(new DrawingHistory());
  const locked = useRef(false);
  const activeImage = useRef(imageId);
  activeImage.current = imageId;
  const mutation = useMutation({
    mutationFn: async ({
      photo,
      command,
      rect,
      snapshot,
    }: {
      photo: string;
      command: 'draw' | 'undo' | 'redo';
      rect?: BoxRect;
      snapshot: Detection[];
    }) => {
      const key = reviewKeys.image(surveyId, photo);
      const patch = (change: (detections: Detection[]) => Detection[]) =>
        qc.setQueryData<ReviewImageDetail>(key, (previous) =>
          previous ? { ...previous, detections: change(previous.detections) } : previous,
        );
      return history.current.execute(
        photo,
        command,
        {
          add: async (id, box) => {
            const created = await wfpApi.addDetection(id, box);
            await qc.cancelQueries({ queryKey: key });
            patch((detections) => [...detections.filter((d) => d.id !== created.id), created]);
            return created;
          },
          remove: async (id) => {
            await wfpApi.deleteDetection(id);
            await qc.cancelQueries({ queryKey: key });
            patch((detections) => detections.filter((d) => d.id !== id));
          },
        },
        snapshot,
        rect,
      );
    },
    onSettled: (_data, _error, variables) =>
      Promise.all([qc.invalidateQueries({ queryKey: reviewKeys.image(surveyId, variables.photo) }), refresh()]),
  });
  const run = (command: 'draw' | 'undo' | 'redo', rect?: BoxRect, onCreated?: (d: Detection) => void) => {
    if (!imageId || locked.current) return;
    locked.current = true;
    mutation.mutate(
      { photo: imageId, command, rect, snapshot: boxes },
      {
        onSuccess: (created) => {
          if (created && activeImage.current === imageId) onCreated?.(created);
        },
        onSettled: () => {
          locked.current = false;
        },
      },
    );
  };
  return {
    run,
    isPending: mutation.isPending,
    isBusy: () => locked.current,
    error: mutation.error,
    canUndo: Boolean(imageId && history.current.canUndo(imageId, boxes)),
    canRedo: Boolean(imageId && history.current.canRedo(imageId)),
  };
}
