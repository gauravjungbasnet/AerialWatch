/**
 * Neutral annotation geometry (docs/17 §3a). No module, API or React imports: any app can use it.
 * Coordinates are original-image pixels with a top-left origin; zoom never changes them.
 * The annotation record drawn by the viewer is `ViewerAnnotation` in `../viewer/ImageViewer.tsx`.
 */
export interface Rect {
  x: number;
  y: number;
  width: number;
  height: number;
}

/** A box edit: `resize` changes the size, `move` only the position (callers sync resizes between panes, not moves). */
export type ChangeKind = 'move' | 'resize';
