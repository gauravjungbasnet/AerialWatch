import type { Annotation, Rect } from '@shared/imaging/annotations/types';
import type { MapPoint } from '@shared/maps/types';

/**
 * Synthetic data for the shared-components example (docs/18 step 7). Generated in code: no survey imagery, no API and no
 * module records, so it proves the shared viewer, comparison and map work for any app.
 */

export const SCENE = { width: 800, height: 520 };
export const OBJECTS: Rect[] = [
  { x: 150, y: 120, width: 70, height: 46 },
  { x: 420, y: 260, width: 90, height: 60 },
  { x: 610, y: 90, width: 60, height: 40 },
];

/** A seeded pseudo-random scene: "colour" is green ground with darker patches, "heat" is dark with warm objects. */
export function sceneImage(kind: 'colour' | 'heat'): string {
  let seed = 7;
  const rand = () => (seed = (seed * 16807) % 2147483647) / 2147483647;
  const ground = kind === 'colour' ? '#5b7f3a' : '#1f2328';
  const patch = kind === 'colour' ? '#3f5f28' : '#2c3138';
  const object = kind === 'colour' ? '#6b4f3a' : '#f5f0e6';
  const patches = Array.from({ length: 140 }, () => {
    const r = 6 + rand() * 18;
    return `<circle cx="${(rand() * SCENE.width).toFixed(1)}" cy="${(rand() * SCENE.height).toFixed(1)}" r="${r.toFixed(1)}" fill="${patch}" opacity="0.7"/>`;
  }).join('');
  const objects = OBJECTS.map(
    (o) =>
      `<ellipse cx="${o.x + o.width / 2}" cy="${o.y + o.height / 2}" rx="${o.width / 3}" ry="${o.height / 4}" fill="${object}"/>`,
  ).join('');
  const svg = `<svg xmlns="http://www.w3.org/2000/svg" width="${SCENE.width}" height="${SCENE.height}" viewBox="0 0 ${SCENE.width} ${SCENE.height}"><rect width="100%" height="100%" fill="${ground}"/>${patches}${objects}</svg>`;
  return `data:image/svg+xml;charset=utf-8,${encodeURIComponent(svg)}`;
}

/** In-memory records an imaginary app keeps; the adapter below is all it needs to use the shared viewer. */
export interface ExampleMark extends Rect {
  id: string;
  checked: boolean;
}

export const INITIAL_MARKS: ExampleMark[] = OBJECTS.map((o, i) => ({ ...o, id: `m${i + 1}`, checked: i === 0 }));

/** Example adapter: records → shared `Annotation`s (checked = solid teal, unchecked = dashed amber). */
export function toAnnotations(marks: ExampleMark[]): Annotation[] {
  return marks.map((m, i) => ({
    id: m.id,
    x: m.x,
    y: m.y,
    width: m.width,
    height: m.height,
    label: `${i + 1} · ${m.checked ? 'checked' : 'to check'}`,
    appearance: m.checked ? { color: 'var(--color-teal-400)' } : { color: 'var(--color-amber-400)', dash: [6, 4] },
  }));
}

/** Map points around Darwin Harbour. */
export const EXAMPLE_POINTS: MapPoint[] = [
  [-12.4634, 130.8456, 'Point A', 3],
  [-12.4381, 130.8411, 'Point B', 0],
  [-12.452, 130.817, 'Point C', 7],
  [-12.479, 130.87, 'Point D', 1],
].map(([lat, lng, name, count]) => ({
  id: String(name),
  lat: Number(lat),
  lng: Number(lng),
  radius: 5 + Number(count),
  stroke: '#ffffff',
  fill: Number(count) > 0 ? '#0d9488' : '#9ca3af',
  weight: 2,
  fillOpacity: 0.85,
  tooltip: { title: String(name), lines: [`${count} items`] },
  label: `Select ${name}, ${count} items`,
}));
