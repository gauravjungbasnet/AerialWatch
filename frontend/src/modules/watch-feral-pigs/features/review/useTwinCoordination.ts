import { useMemo } from 'react';
import { followResize } from '@shared/imaging/annotations/geometry';
import type { Rect } from '@shared/imaging/annotations/types';
import type { Detection, ReviewImageDetail, TwinMark } from '../../api/types';
import {
  countedBoxAnnotations,
  markIdOfSighting,
  sightingId,
  sightingSuggestionAnnotations,
} from '../../adapters/annotations';
import { cameraName } from '../../domain/cameras';
import { useTwinCheck, useTwinMarkActions } from './twinHooks';

interface TwinCoordinationInput {
  surveyId: string;
  photoId: string | undefined;
  detail: ReviewImageDetail | undefined;
  detailUpdatedAt: number;
  boxes: Detection[];
  editable: boolean;
  selectedId: string | null;
  select(id: string): void;
  /** Saves a move or resize of a counted box. */
  updateBox(id: string, rect: Rect): void;
  onDrawn(): void;
}

/**
 * Keeps the counted photo and its colour/thermal twin in step (D-039–D-041): the boxes drawn on the counted photo,
 * resize-follows-resize between the two photos (a move is not mirrored), drawing on the twin, and confirming a pig that
 * was first drawn on the twin.
 */
export function useTwinCoordination({
  surveyId,
  photoId,
  detail,
  detailUpdatedAt,
  boxes,
  editable,
  selectedId,
  select,
  updateBox,
  onDrawn,
}: TwinCoordinationInput) {
  const photo = detail?.image;
  const twin = detail?.twin;
  const twinCheck = useTwinCheck(surveyId, photoId, detail?.detections ?? NO_BOXES, detailUpdatedAt, Boolean(twin));
  const twinActions = useTwinMarkActions(surveyId, photoId);
  const twinName = twin ? cameraName(twin.camera).toLowerCase() : 'twin';
  const marks = twinCheck.data?.marks;
  const sightings = twinCheck.data?.sightings;

  // Pigs drawn on the twin first are suggested on this photo as dashed boxes; they aren't counted until confirmed.
  const countedAnnotations = useMemo(
    () => [...countedBoxAnnotations(boxes), ...sightingSuggestionAnnotations(sightings, twinName)],
    [boxes, sightings, twinName],
  );

  const placedRect = (m: TwinMark): Rect | null =>
    m.state === 'confirmed' && m.x != null && m.y != null && m.width != null && m.height != null
      ? { x: m.x, y: m.y, width: m.width, height: m.height }
      : null;

  return {
    twin,
    twinCheck,
    twinActions,
    countedAnnotations,
    /** A box on the counted photo moved or resized. Moving a suggestion from the twin confirms it here (it is counted). */
    changeCountedBox(id: string, rect: Rect) {
      const sighting = sightings?.find((s) => s.markId === markIdOfSighting(id));
      if (sighting) return twinActions.confirmOnCounted(sighting, rect);
      const before = boxes.find((b) => b.id === id);
      updateBox(id, rect);
      const mark = marks?.find((m) => m.detectionId === id);
      const partnerBefore = mark && placedRect(mark);
      if (!before || !mark || !partnerBefore) return;
      const partner = followResize(before, rect, partnerBefore, { w: twin?.widthPx, h: twin?.heightPx });
      if (partner) twinActions.place(mark, partner);
    },
    /** A pig's box on the twin was placed or moved; resizing a placed one also resizes the counted box. */
    changeTwinBox(mark: TwinMark, rect: Rect) {
      twinActions.place(mark, rect);
      const before = placedRect(mark);
      const counted = boxes.find((b) => b.id === mark.detectionId);
      if (!before || !counted) return;
      const partner = followResize(before, rect, counted, { w: photo?.widthPx, h: photo?.heightPx });
      if (partner) updateBox(counted.id, partner);
    },
    /** Drawing on the twin places the selected pig if it isn't placed yet; otherwise it adds a sighting. */
    drawOnTwin(rect: Rect) {
      if (!editable) return;
      const mark = marks?.find((m) => m.detectionId === selectedId && m.state !== 'confirmed');
      if (mark) return twinActions.place(mark, rect);
      twinActions.addSighting.mutate(rect, {
        onSuccess: (s) => {
          select(sightingId(s.markId));
          onDrawn();
        },
      });
    },
  };
}

const NO_BOXES: Detection[] = [];
