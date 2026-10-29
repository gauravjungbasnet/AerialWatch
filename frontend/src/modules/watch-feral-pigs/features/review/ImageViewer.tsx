import { forwardRef } from 'react';
import type { Annotation, Rect } from '@shared/imaging/annotations/types';
import {
  ImageViewer as SharedImageViewer,
  type ImageViewerHandle,
  type ViewerMode,
} from '@shared/imaging/viewer/ImageViewer';
import { MIN_BOX_PX } from '../../domain/boxAppearance';

export type { ImageViewerHandle, ViewerMode } from '@shared/imaging/viewer/ImageViewer';

interface ImageViewerProps {
  src: string;
  alt: string;
  /** Built by `adapters/annotations.ts`. */
  annotations: Annotation[];
  selectedId: string | null;
  onSelect(id: string | null): void;
  mode: ViewerMode;
  /** False once the review is complete: boxes can be selected but not drawn, moved or resized. */
  editable: boolean;
  onCreate(rect: Rect): void;
  onChangeBox(id: string, rect: Rect): void;
}

/** Review preset of the shared viewer: zoom, pan and selection always; drawing and editing only while editable. */
export const ImageViewer = forwardRef<ImageViewerHandle, ImageViewerProps>(function ImageViewer(
  { editable, ...props },
  ref,
) {
  return (
    <SharedImageViewer
      ref={ref}
      {...props}
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
