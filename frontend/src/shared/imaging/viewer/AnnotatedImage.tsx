import type { Rect } from '../annotations/types';
import { ImageViewer } from './ImageViewer';

export interface ImageAnnotation extends Rect {
  key: string;
  label: string;
  color: string;
  dashed?: boolean;
}
export interface AnnotatedImageProps {
  src: string;
  alt: string;
  caption: string;
  widthPx?: number;
  heightPx?: number;
  marks: ImageAnnotation[];
}

/** Read-only preset of the same viewer used by editing workflows. */
export function AnnotatedImage({ src, alt, caption, marks }: AnnotatedImageProps) {
  return (
    <figure className="min-w-0">
      <div className="h-64 overflow-hidden rounded-2xl bg-gray-900 sm:h-80">
        <ImageViewer
          src={src}
          alt={alt}
          options={{ labels: 'all' }}
          annotations={marks.map((mark) => ({
            ...mark,
            id: mark.key,
            appearance: { color: mark.color, dash: mark.dashed ? [6, 4] : undefined },
          }))}
        />
      </div>
      <figcaption className="mt-2 truncate px-2 text-xs text-gray-600 dark:text-gray-400" title={caption}>
        {caption}
      </figcaption>
    </figure>
  );
}
