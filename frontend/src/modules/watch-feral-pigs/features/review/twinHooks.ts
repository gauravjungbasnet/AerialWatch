import { keepPreviousData, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import type { BoxRect, Detection, SetTwinMarkRequest, TwinMark, TwinSighting } from '../../api/types';
import { reviewKeys, twinKeys } from '../../api/keys';

/** Viewer id of a twin sighting's box, shared by both photos so selecting it on one highlights it on the other. */
export const sightingBoxId = (markId: string) => `sighting:${markId}`;
export const markIdOfSightingBox = (boxId: string) =>
  boxId.startsWith('sighting:') ? boxId.slice('sighting:'.length) : null;

const rectOf = (x?: number, y?: number, width?: number, height?: number): BoxRect | undefined =>
  x != null && y != null && width != null && height != null ? { x, y, width, height } : undefined;

/**
 * Twin check for one counted photo (D-040, D-041). Refetches whenever the photo's boxes change, keyed on the time the
 * box list was last loaded, so it also refetches after the server has finished a change (the box list is updated on
 * screen first, before the server, and asking too early would bring back a box that was just deleted). Marks of boxes
 * no longer confirmed on screen are hidden straight away.
 */
export function useTwinCheck(
  surveyId: string,
  imageId: string | undefined,
  boxes: Detection[],
  boxesUpdatedAt: number,
  enabled: boolean,
) {
  const confirmed = boxes
    .filter((b) => b.status === 'accepted')
    .map((b) => `${b.id}:${b.x},${b.y},${b.width},${b.height}`)
    .join('|');
  const confirmedIds = new Set(boxes.filter((b) => b.status === 'accepted').map((b) => b.id));
  return useQuery({
    queryKey: twinKeys.photo(surveyId, imageId ?? '', confirmed, boxesUpdatedAt),
    queryFn: () => wfpApi.twinCheck(imageId!),
    enabled: Boolean(imageId) && enabled,
    placeholderData: keepPreviousData,
    select: (check) => ({ ...check, marks: check.marks.filter((m) => confirmedIds.has(m.detectionId)) }),
  });
}

// Compatibility export; geometry is independent of the WFP workflow.
export { followResize } from '@shared/imaging/annotations/geometry';

export function useTwinMarkActions(surveyId: string, imageId: string | undefined) {
  const qc = useQueryClient();
  const refresh = () =>
    Promise.all([
      qc.invalidateQueries({ queryKey: twinKeys.photoAll(surveyId, imageId ?? '') }),
      qc.invalidateQueries({ queryKey: twinKeys.summary(surveyId) }),
    ]);
  // Confirming a sighting creates a counted pig, so the review (boxes, progress, results) refreshes too.
  const refreshAll = () => Promise.all([refresh(), qc.invalidateQueries({ queryKey: reviewKeys.all(surveyId) })]);

  const set = useMutation({
    mutationFn: ({ id, body }: { id: string; body: SetTwinMarkRequest }) => wfpApi.setTwinMark(id, body),
    onSettled: refresh,
  });
  const clear = useMutation({ mutationFn: (id: string) => wfpApi.clearTwinMark(id), onSettled: refresh });
  const addSighting = useMutation({
    mutationFn: (box: BoxRect) => wfpApi.addTwinSighting(imageId!, box),
    onSettled: refresh,
  });
  const confirmSighting = useMutation({
    mutationFn: ({ markId, box }: { markId: string; box: BoxRect }) => wfpApi.confirmTwinSighting(markId, box),
    onSettled: refreshAll,
  });
  const sightingVisibility = useMutation({
    mutationFn: ({ markId, notVisible }: { markId: string; notVisible: boolean }) =>
      wfpApi.setTwinSightingVisibility(markId, notVisible),
    onSettled: refresh,
  });
  const removeSighting = useMutation({
    mutationFn: (markId: string) => wfpApi.removeTwinSighting(markId),
    onSettled: refresh,
  });

  return {
    set,
    clear,
    addSighting,
    confirmSighting,
    sightingVisibility,
    removeSighting,
    /** Errors from any twin action, for the review page's error banner. */
    mutations: [set, clear, addSighting, confirmSighting, sightingVisibility, removeSighting],
    /** Confirm a suggested or moved position of a counted pig on the twin. */
    place: (mark: TwinMark, rect?: BoxRect) => {
      const r = rect ?? rectOf(mark.x, mark.y, mark.width, mark.height);
      if (r) set.mutate({ id: mark.detectionId, body: { state: 'confirmed', ...r } });
    },
    notVisible: (mark: TwinMark) => set.mutate({ id: mark.detectionId, body: { state: 'notVisible' } }),
    /** Confirm a sighting on the counted photo at its suggested (or moved) position: it becomes a counted pig. */
    confirmOnCounted: (s: TwinSighting, rect?: BoxRect) => {
      const r = rect ?? rectOf(s.x, s.y, s.width, s.height);
      if (r) confirmSighting.mutate({ markId: s.markId, box: r });
    },
  };
}

/** Sightings still suggested on the counted photo, as dashed boxes for its viewer (D-041). */
export function sightingSuggestions(sightings: TwinSighting[] | undefined): Detection[] {
  return (sightings ?? [])
    .filter(
      (s) => s.state === 'suggestedOnCounted' && s.x != null && s.y != null && s.width != null && s.height != null,
    )
    .map(
      (s) =>
        ({
          id: sightingBoxId(s.markId),
          imageId: '',
          x: s.x!,
          y: s.y!,
          width: s.width!,
          height: s.height!,
          label: 'pig',
          source: 'ai',
          status: 'pending',
        }) as Detection,
    );
}
