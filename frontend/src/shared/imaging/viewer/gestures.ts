/** Modifier drawing wins over box selection/resize, without changing the selected toolbar tool. */
export const drawGestureRequested = (mode: 'select' | 'draw', editable: boolean, ctrlKey: boolean, metaKey: boolean) =>
  editable && (mode === 'draw' || ctrlKey || metaKey);
