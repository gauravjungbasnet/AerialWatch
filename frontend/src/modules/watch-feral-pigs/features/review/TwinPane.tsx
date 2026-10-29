import { forwardRef } from 'react';
import type { BoxRect, Detection, TwinImage, TwinMark, TwinSighting } from '../../api/types';
import { wfpApi } from '../../api/client';
import { cameraName } from '../../domain/cameras';
import { ImageViewer, type ImageViewerHandle, type ViewerMode } from './ImageViewer';
import { markIdOfSightingBox, sightingBoxId } from './twinHooks';

interface TwinPaneProps {
  twin: TwinImage;
  marks: TwinMark[];
  sightings: TwinSighting[];
  selectedId: string | null;
  onSelect(id: string | null): void;
  mode: ViewerMode;
  editable: boolean;
  /** A counted pig's position on the twin was confirmed or moved. */
  onPlace(mark: TwinMark, rect: BoxRect): void;
  /** A new box drawn on the twin (Draw tool or Ctrl/⌘ + drag). */
  onDraw(rect: BoxRect): void;
}

/** Counted pigs: suggested = dashed amber, placed = solid sky. Sightings drawn here: solid sky with their c-label. */
function toBoxes(marks: TwinMark[], sightings: TwinSighting[]): Detection[] {
  const pigs = marks
    .filter((m) => m.state !== 'notVisible' && m.x != null && m.y != null && m.width != null && m.height != null)
    .map(
      (m) =>
        ({
          id: m.detectionId,
          imageId: '',
          x: m.x!,
          y: m.y!,
          width: m.width!,
          height: m.height!,
          label: 'pig',
          source: m.state === 'confirmed' ? 'ranger' : 'ai',
          status: m.state === 'confirmed' ? 'accepted' : 'pending',
        }) as Detection,
    );
  const drawn = sightings.map(
    (s) =>
      ({
        id: sightingBoxId(s.markId),
        imageId: '',
        x: s.twinX,
        y: s.twinY,
        width: s.twinWidth,
        height: s.twinHeight,
        label: 'pig',
        source: 'ranger',
        status: 'accepted',
      }) as Detection,
  );
  return [...pigs, ...drawn];
}

/**
 * The other camera's photo (D-040, D-041). Shows each counted pig's position here, and pigs drawn here first. Selection
 * is shared with the counted photo (same ids), so pig 2 or c1 is highlighted on both. Dragging a pig's box places it;
 * drawing a new box places the selected pig if it isn't placed yet, otherwise adds a sighting that is then suggested on
 * the counted photo.
 */
export const TwinPane = forwardRef<ImageViewerHandle, TwinPaneProps>(function TwinPane(
  { twin, marks, sightings, selectedId, onSelect, mode, editable, onPlace, onDraw },
  ref,
) {
  const byId = new Map(marks.map((m) => [m.detectionId, m]));
  const bySighting = new Map(sightings.map((s) => [s.markId, s]));
  return (
    <ImageViewer
      ref={ref}
      src={wfpApi.surveyImageUrl(twin.id)}
      alt={`${cameraName(twin.camera)} twin ${twin.fileName}`}
      boxes={toBoxes(marks, sightings)}
      selectedId={selectedId}
      onSelect={onSelect}
      mode={mode}
      editable={editable}
      onCreate={onDraw}
      onChangeBox={(id, rect) => {
        const mark = byId.get(id);
        if (mark) onPlace(mark, rect);
      }}
      caption={(b) => {
        const sid = markIdOfSightingBox(b.id);
        if (sid) return `${bySighting.get(sid)?.label ?? ''} · drawn here`;
        const m = byId.get(b.id);
        if (!m) return '';
        return m.state === 'confirmed' ? `${m.number} · ✓ placed` : `${m.number} · suggested`;
      }}
    />
  );
});
