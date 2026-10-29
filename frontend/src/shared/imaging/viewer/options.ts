export interface ViewerOptions {
  zoom?: boolean;
  pan?: boolean;
  draw?: boolean;
  move?: boolean;
  resize?: boolean;
  selection?: boolean;
  labels?: 'selected' | 'all' | 'none';
  showHint?: boolean;
  minBoxSize?: number;
}

/** Safe by default: capabilities require both opt-in and the corresponding callback. */
export function resolveViewerOptions(
  options: ViewerOptions = {},
  callbacks: { create: boolean; change: boolean; select: boolean },
) {
  return {
    zoom: options.zoom ?? false,
    pan: options.pan ?? false,
    draw: Boolean(options.draw && callbacks.create),
    move: Boolean(options.move && callbacks.change),
    resize: Boolean(options.resize && callbacks.change),
    selection: Boolean(options.selection && callbacks.select),
    labels: options.labels ?? 'selected',
    showHint: options.showHint ?? false,
    minBoxSize: Math.max(1, options.minBoxSize ?? 4),
  };
}
