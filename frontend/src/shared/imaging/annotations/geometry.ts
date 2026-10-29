import type { ChangeKind, Rect } from './types';

export const clamp = (v: number, lo: number, hi: number) => Math.min(hi, Math.max(lo, v));

/** Round to one decimal place, the precision boxes are stored at. */
export const round1 = (v: number) => Math.round(v * 10) / 10;

export const roundRect = (r: Rect): Rect => ({
  x: round1(r.x),
  y: round1(r.y),
  width: round1(r.width),
  height: round1(r.height),
});

/** Keep a rectangle inside an image of the given size. */
export function clampRect(r: Rect, imageW: number, imageH: number): Rect {
  const x = clamp(r.x, 0, imageW);
  const y = clamp(r.y, 0, imageH);
  return { x, y, width: clamp(r.width, 0, imageW - x), height: clamp(r.height, 0, imageH - y) };
}

/** Rectangle spanned by two corner points (any drag direction), kept inside the image. */
export function rectFromPoints(
  a: { x: number; y: number },
  b: { x: number; y: number },
  imageW: number,
  imageH: number,
): Rect {
  return clampRect(
    { x: Math.min(a.x, b.x), y: Math.min(a.y, b.y), width: Math.abs(a.x - b.x), height: Math.abs(a.y - b.y) },
    imageW,
    imageH,
  );
}

/** Large enough to keep (both sides at least `min` pixels). */
export const isBigEnough = (r: Rect, min: number) => r.width >= min && r.height >= min;

/** A change is a move when the size is unchanged, otherwise a resize. */
export function changeKind(before: Rect, after: Rect): ChangeKind {
  return before.width === after.width && before.height === after.height ? 'move' : 'resize';
}

/**
 * Resizing one box resizes its partner on another photo: the same edge changes, scaled by the two boxes' size ratio.
 * A pure move returns null: moving corrects a position on one photo only (D-041).
 */
export function followResize(
  before: Rect,
  after: Rect,
  partner: Rect,
  limit?: { w?: number; h?: number },
): Rect | null {
  if (changeKind(before, after) === 'move') return null;
  if (before.width <= 0 || before.height <= 0) return null;
  const sx = partner.width / before.width;
  const sy = partner.height / before.height;
  const w = Math.max(4, after.width * sx);
  const h = Math.max(4, after.height * sy);
  let x = partner.x + (after.x - before.x) * sx;
  let y = partner.y + (after.y - before.y) * sy;
  if (limit?.w) x = clamp(x, 0, Math.max(0, limit.w - w));
  if (limit?.h) y = clamp(y, 0, Math.max(0, limit.h - h));
  return roundRect({ x, y, width: Math.min(w, limit?.w ?? w), height: Math.min(h, limit?.h ?? h) });
}
