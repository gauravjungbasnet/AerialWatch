import type { MapPadding } from './types';

interface Box {
  left: number;
  top: number;
  right: number;
  bottom: number;
}

/**
 * How far floating panels reach into the map from each side (docs/18 step 5: measured, not fixed pixel offsets).
 * Each overlay counts against the side it covers least: a tall panel on the left is a left inset, a sheet along the
 * bottom is a bottom inset. Overlays that don't touch the map are ignored.
 */
export function coveredPadding(map: Box, overlays: Box[]): MapPadding {
  const padding: MapPadding = { top: 0, right: 0, bottom: 0, left: 0 };
  for (const o of overlays) {
    const left = Math.max(o.left, map.left);
    const right = Math.min(o.right, map.right);
    const top = Math.max(o.top, map.top);
    const bottom = Math.min(o.bottom, map.bottom);
    if (right <= left || bottom <= top) continue;
    const cuts: MapPadding = {
      left: right - map.left,
      right: map.right - left,
      top: bottom - map.top,
      bottom: map.bottom - top,
    };
    const side = (Object.keys(cuts) as (keyof MapPadding)[]).reduce((a, b) => (cuts[b] < cuts[a] ? b : a));
    padding[side] = Math.max(padding[side], cuts[side]);
  }
  return padding;
}

/** Adds a margin on every side and makes sure at least `keep` of the width and height stays free. */
export function withMargin(padding: MapPadding, margin: number, size: { x: number; y: number }, keep = 0.25) {
  const fit = (a: number, b: number, length: number): [number, number] => {
    const max = length * (1 - keep);
    const k = a + b > max && a + b > 0 ? max / (a + b) : 1;
    return [a * k, b * k];
  };
  const [left, right] = fit(padding.left + margin, padding.right + margin, size.x);
  const [top, bottom] = fit(padding.top + margin, padding.bottom + margin, size.y);
  return { top, right, bottom, left };
}
