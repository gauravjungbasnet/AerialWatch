/**
 * Neutral annotation contracts (docs/17 §3a, docs/18 step 3). No module, API or React imports: any app can use them.
 * Coordinates are original-image pixels with a top-left origin; zoom never changes them.
 */
export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

/** A box edit: `resize` changes the size, `move` only the position (callers sync resizes between panes, not moves). */
export type ChangeKind = 'move' | 'resize';

/** How a box is drawn. Never rely on colour alone: give different states different dash patterns or labels too. */
export interface AnnotationAppearance {
  /** CSS colour, e.g. a Tailwind palette variable so light and dark themes stay consistent. */
  color: string;
  fill?: string;
  /** Dash pattern in screen pixels; solid when missing. */
  dash?: number[];
  opacity?: number;
}

/**
 * The one record every viewer draws (editable review panes and read-only galleries alike).
 * `id` is the selection key: annotations in different panes may share it so selecting one highlights both.
 * Modules build these from their own records in an adapter; the viewer never sees module types.
 */
export interface Annotation extends Rect {
  id: string;
  label: string;
  appearance: AnnotationAppearance;
  can?: { move?: boolean; resize?: boolean };
}
