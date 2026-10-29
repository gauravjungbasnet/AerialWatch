import type { Detection } from '../api/types';
import { percent } from '@shared/lib/format';

/** Smallest box side in original pixels; same rule as the API (ReviewRules.MinBoxSizePx). */
export const MIN_BOX_PX = 4;

export interface BoxStyle {
  /** CSS colour (Tailwind palette variables, so light/dark themes stay consistent). */
  color: string;
  fill: string;
  /** Dash pattern in screen pixels; solid when undefined. Never colour alone: each state also has its own line style and caption. */
  dash?: number[];
  opacity: number;
  label: string;
}

/** Pending = amber dashed, accepted = green solid, drawn by a ranger = sky solid, rejected = red dotted and faded. */
export function boxStyle(b: Pick<Detection, 'source' | 'status'>): BoxStyle {
  if (b.status === 'rejected')
    return {
      color: 'var(--color-red-500)',
      fill: 'color-mix(in srgb, var(--color-red-500) 15%, transparent)',
      dash: [2, 4],
      opacity: 0.75,
      label: 'Rejected',
    };
  if (b.source === 'ranger')
    return {
      color: 'var(--color-sky-400)',
      fill: 'color-mix(in srgb, var(--color-sky-400) 18%, transparent)',
      opacity: 1,
      label: 'Added by ranger',
    };
  if (b.status === 'accepted')
    return {
      color: 'var(--color-green-400)',
      fill: 'color-mix(in srgb, var(--color-green-400) 18%, transparent)',
      opacity: 1,
      label: 'Accepted',
    };
  return {
    color: 'var(--color-amber-400)',
    fill: 'color-mix(in srgb, var(--color-amber-400) 18%, transparent)',
    dash: [6, 4],
    opacity: 1,
    label: 'To check',
  };
}

/** Short caption drawn above a box, e.g. "AI 87%", "✓ AI 87%", "✓ added", "✗ AI 31%". */
export function boxCaption(b: Pick<Detection, 'source' | 'status' | 'confidence'>): string {
  const ai = b.confidence != null ? `AI ${percent(b.confidence)}` : 'AI';
  if (b.source === 'ranger') return '✓ added';
  if (b.status === 'accepted') return `✓ ${ai}`;
  if (b.status === 'rejected') return `✗ ${ai}`;
  return ai;
}
