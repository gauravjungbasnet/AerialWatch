import type { Annotation, AnnotationAppearance, Rect } from '@shared/imaging/annotations/types';
import type { Detection, TwinCheck, TwinMark, TwinSighting } from '../api/types';
import { boxCaption, boxStyle } from '../domain/boxAppearance';

/**
 * WFP records → the shared viewer's `Annotation` (docs/18 step 3). The only place that decides how pigs, twin marks
 * and twin sightings look and are labelled; the review panes and the results gallery all draw through here.
 * Selection ids: a counted box and its twin mark share the detection id; a twin sighting is `sighting:<markId>` on
 * both photos (D-041), so selecting it on one photo highlights it on the other.
 */

const SIGHTING_PREFIX = 'sighting:';
export const sightingId = (markId: string) => `${SIGHTING_PREFIX}${markId}`;
export const markIdOfSighting = (id: string) =>
  id.startsWith(SIGHTING_PREFIX) ? id.slice(SIGHTING_PREFIX.length) : null;

/** Same look as an AI suggestion still to check (amber, dashed) and a ranger's box (sky, solid). */
const suggested = boxStyle({ source: 'ai', status: 'pending' });
const placed = boxStyle({ source: 'ranger', status: 'accepted' });
/** Results gallery: a pig placed on the twin photo, and a pig seen only on the twin photo. */
const EVIDENCE_TWIN: AnnotationAppearance = { color: 'var(--color-violet-400)' };
const EVIDENCE_ONLY_ON_TWIN: AnnotationAppearance = { color: 'var(--color-amber-400)', dash: [6, 4] };

const rectOf = (x?: number, y?: number, width?: number, height?: number): Rect | null =>
  x != null && y != null && width != null && height != null ? { x, y, width, height } : null;

const annotation = (id: string, rect: Rect, label: string, appearance: AnnotationAppearance): Annotation => ({
  id,
  x: rect.x,
  y: rect.y,
  width: rect.width,
  height: rect.height,
  label,
  appearance,
});

// ---- Review: counted photo ----

/** Every box on the counted photo, numbered like the box list: "2 · ✓ AI 87%". */
export function countedBoxAnnotations(boxes: Detection[]): Annotation[] {
  return boxes.map((b, i) => annotation(b.id, b, `${i + 1} · ${boxCaption(b)}`, boxStyle(b)));
}

/** Pigs drawn on the twin first and not yet confirmed here: dashed suggestions "c1 · from colour (rgb)" (D-041). */
export function sightingSuggestionAnnotations(sightings: TwinSighting[] | undefined, twinName: string): Annotation[] {
  return (sightings ?? []).flatMap((s) => {
    const rect = s.state === 'suggestedOnCounted' ? rectOf(s.x, s.y, s.width, s.height) : null;
    return rect ? [annotation(sightingId(s.markId), rect, `${s.label} · from ${twinName}`, suggested)] : [];
  });
}

// ---- Review: twin photo ----

/** Counted pigs on the twin photo (suggested = amber dashed, placed = sky) and pigs drawn there first (D-040, D-041). */
export function twinPaneAnnotations(marks: TwinMark[], sightings: TwinSighting[]): Annotation[] {
  const pigs = marks.flatMap((m) => {
    const rect = m.state === 'notVisible' ? null : rectOf(m.x, m.y, m.width, m.height);
    if (!rect) return [];
    return m.state === 'confirmed'
      ? [annotation(m.detectionId, rect, `${m.number} · ✓ placed`, placed)]
      : [annotation(m.detectionId, rect, `${m.number} · suggested`, suggested)];
  });
  const drawnHere = sightings.map((s) =>
    annotation(
      sightingId(s.markId),
      { x: s.twinX, y: s.twinY, width: s.twinWidth, height: s.twinHeight },
      `${s.label} · drawn here`,
      placed,
    ),
  );
  return [...pigs, ...drawnHere];
}

// ---- Results: read-only evidence ----

/** Confirmed pigs only, numbered by their place in the photo's full box list (same numbers as the review tool). */
export function confirmedBoxAnnotations(boxes: Detection[]): Annotation[] {
  return boxes.flatMap((b, i) =>
    b.status === 'accepted' ? [annotation(b.id, b, String(i + 1), { color: boxStyle(b).color })] : [],
  );
}

/** The twin photo in Results: pigs a ranger placed there (violet, numbered) and pigs seen only there (amber dashed). */
export function twinEvidenceAnnotations(check: TwinCheck | undefined): Annotation[] {
  if (!check) return [];
  const placedPigs = check.marks.flatMap((m) => {
    const rect = m.state === 'confirmed' ? rectOf(m.x, m.y, m.width, m.height) : null;
    return rect ? [annotation(m.detectionId, rect, String(m.number), EVIDENCE_TWIN)] : [];
  });
  const onlyHere = check.sightings
    .filter((s) => s.state === 'notVisibleOnCounted')
    .map((s) =>
      annotation(
        sightingId(s.markId),
        { x: s.twinX, y: s.twinY, width: s.twinWidth, height: s.twinHeight },
        s.label,
        EVIDENCE_ONLY_ON_TWIN,
      ),
    );
  return [...placedPigs, ...onlyHere];
}
