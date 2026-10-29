import type { Annotation } from '../annotations/types';
import { ImageViewer } from './ImageViewer';

export interface AnnotatedImageProps {
  src: string;
  alt: string;
  caption: string;
  widthPx?: number;
  heightPx?: number;
  annotations: Annotation[];
}

/** Read-only preset of the same viewer used by editing workflows: every label shown, no zoom, pan or editing. */
export function AnnotatedImage({ src, alt, caption, annotations }: AnnotatedImageProps) {
  return (
    <figure className="min-w-0">
      <div className="h-64 overflow-hidden rounded-2xl bg-gray-900 sm:h-80">
        <ImageViewer src={src} alt={alt} options={{ labels: 'all' }} annotations={annotations} />
      </div>
      <figcaption className="mt-2 truncate px-2 text-xs text-gray-600 dark:text-gray-400" title={caption}>
        {caption}
      </figcaption>
    </figure>
  );
}
