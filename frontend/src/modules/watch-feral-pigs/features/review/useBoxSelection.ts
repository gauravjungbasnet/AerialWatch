import { useEffect, useState } from 'react';
import type { Detection } from '../../api/types';

/**
 * The selected box (or twin sighting) id. On a new photo the first suggestion still to check is selected, so A / R
 * work straight away; a selection that still exists is kept.
 */
export function useBoxSelection(boxes: Detection[]) {
  const [selectedId, setSelectedId] = useState<string | null>(null);
  useEffect(() => {
    setSelectedId((current) =>
      current && boxes.some((b) => b.id === current)
        ? current
        : (boxes.find((b) => b.status === 'pending')?.id ?? null),
    );
  }, [boxes]);

  /** Next / previous box on the photo, wrapping round. */
  const selectBox = (delta: number) => {
    if (boxes.length === 0) return;
    const i = boxes.findIndex((b) => b.id === selectedId);
    setSelectedId(boxes[(i + delta + boxes.length) % boxes.length].id);
  };

  return { selectedId, setSelectedId, selected: boxes.find((b) => b.id === selectedId) ?? null, selectBox };
}
