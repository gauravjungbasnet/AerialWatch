import { forwardRef } from 'react';
import type { BoxRect, Detection } from '../../api/types';
import {
  ImageViewer as SharedImageViewer,
  type ImageViewerHandle,
  type ViewerMode,
} from '@shared/imaging/viewer/ImageViewer';
import { boxCaption, boxStyle, MIN_BOX_PX } from './boxStyles';

export type { ImageViewerHandle, ViewerMode } from '@shared/imaging/viewer/ImageViewer';

interface ImageViewerProps {
  src: string;
  alt: string;
  boxes: Detection[];
  selectedId: string | null;
  onSelect(id: string | null): void;
  mode: ViewerMode;
  /** False once the review is complete: boxes can be selected but not drawn, moved or resized. */
  editable: boolean;
  onCreate(rect: BoxRect): void;
  onChangeBox(id: string, rect: BoxRect): void;
  /** Optional label above each box; default "n · AI 87%" (the twin check passes its own, D-040). */
  caption?(box: Detection, index: number): string;
}

/** WFP adapter only: shared viewer owns all rendering, gestures and rectangle editing. */
export const ImageViewer = forwardRef<ImageViewerHandle, ImageViewerProps>(function ImageViewer(
  { boxes, caption, editable, ...props },
  ref,
) {
  return (
    <SharedImageViewer
      ref={ref}
      {...props}
      annotations={boxes.map((box, index) => ({
        ...box,
        label: caption ? caption(box, index) : `${index + 1} · ${boxCaption(box)}`,
        appearance: boxStyle(box),
      }))}
      options={{
        zoom: true,
        pan: true,
        selection: true,
        draw: editable,
        move: editable,
        resize: editable,
        labels: 'selected',
        showHint: true,
        minBoxSize: MIN_BOX_PX,
      }}
    />
  );
});
