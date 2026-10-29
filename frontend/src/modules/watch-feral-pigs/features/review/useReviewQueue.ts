import { useEffect, useMemo, useRef } from 'react';
import { useSearchParams } from 'react-router-dom';
import { wfpApi } from '../../api/client';
import type { ReviewImageFilter, SurveyStatus } from '../../api/types';
import { isReviewFilter } from './filters';
import { useReviewImages } from './hooks';

/**
 * The review queue and where the ranger is in it (D-034). The photo and filter live in the URL (?photo=&filter=), so a
 * reload or a shared link opens the same place. Default filter: what still needs checking; everything once reviewed.
 */
export function useReviewQueue(surveyId: string, reviewStatus: SurveyStatus | undefined) {
  const [params, setParams] = useSearchParams();
  const photoId = params.get('photo') ?? undefined;
  const filterParam = params.get('filter');
  const filter: ReviewImageFilter = isReviewFilter(filterParam)
    ? filterParam
    : reviewStatus === 'reviewed'
      ? 'all'
      : 'notReviewed';
  const queue = useReviewImages(surveyId, filter);
  const images = useMemo(() => queue.data ?? [], [queue.data]);
  const index = images.findIndex((i) => i.id === photoId);
  // Where the photo was in the queue, for when it drops out of the filter (e.g. "Not checked" after checking it).
  const lastIndex = useRef(0);
  if (index >= 0) lastIndex.current = index;

  const go = (next: { photo?: string; filter?: ReviewImageFilter }) => {
    const p = new URLSearchParams(params);
    if (next.filter) p.set('filter', next.filter);
    if (next.photo) p.set('photo', next.photo);
    else if (next.filter) p.delete('photo');
    setParams(p, { replace: true });
  };

  // Open the first photo of the queue when none is chosen (or after changing the filter).
  useEffect(() => {
    if (!photoId && images.length > 0 && !queue.isPlaceholderData) go({ photo: images[0].id });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [photoId, images, queue.isPlaceholderData]);

  // Preload the next photo so moving on is instant.
  const nextImage = index >= 0 ? images[index + 1] : undefined;
  useEffect(() => {
    if (nextImage) new Image().src = wfpApi.surveyImageUrl(nextImage.id);
  }, [nextImage]);

  /** Moves through the queue; if the photo left the queue, the one now at its old position is already "next". */
  const step = (delta: number) => {
    if (images.length === 0) return;
    const to = index >= 0 ? index + delta : delta > 0 ? lastIndex.current : lastIndex.current - 1;
    const target = images[Math.min(images.length - 1, Math.max(0, to))];
    if (target && target.id !== photoId) go({ photo: target.id });
  };

  return {
    photoId,
    filter,
    images,
    index,
    isLoading: queue.isLoading,
    isFetching: queue.isFetching,
    canPrev: images.length > 0 && index !== 0,
    canNext: index < images.length - 1,
    /** Where to go after checking this photo; worked out before the queue refreshes. */
    following: images[index + 1] ?? images[index - 1],
    step,
    open: (photo: string) => go({ photo }),
    setFilter: (next: ReviewImageFilter) => go({ filter: next }),
  };
}
