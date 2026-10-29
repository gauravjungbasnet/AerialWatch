import { useEffect, useRef, useState } from 'react';
import { CheckCircle2, ChevronLeft, ChevronRight, Flag } from 'lucide-react';
import { Button, Card } from '@shared/controls';
import type { ReviewImage, ReviewImageFilter } from '../../api/types';
import { FILTERS } from './filters';
import { wfpApi } from '../../api/client';
import { cx } from '@shared/lib/format';

interface PhotoStripProps {
  images: ReviewImage[];
  currentId?: string;
  filter: ReviewImageFilter;
  onFilter(filter: ReviewImageFilter): void;
  onOpen(id: string): void;
  loading?: boolean;
}

/** Filter chips and a scrolling row of thumbnails in flight order; each shows checked, pigs, AI-to-check and flag. */
export function PhotoStrip({ images, currentId, filter, onFilter, onOpen, loading }: PhotoStripProps) {
  const currentRef = useRef<HTMLButtonElement>(null);
  const stripRef = useRef<HTMLOListElement>(null);
  const [scrollable, setScrollable] = useState({ left: false, right: false });
  useEffect(() => {
    const strip = stripRef.current;
    const current = currentRef.current;
    if (strip && current)
      strip.scrollTo({ left: current.offsetLeft - (strip.clientWidth - current.clientWidth) / 2, behavior: 'instant' });
  }, [currentId, images]);
  useEffect(() => {
    const strip = stripRef.current;
    if (!strip) return;
    const update = () =>
      setScrollable({
        left: strip.scrollLeft > 1,
        right: strip.scrollLeft + strip.clientWidth < strip.scrollWidth - 1,
      });
    strip.addEventListener('scroll', update);
    const observer = new ResizeObserver(update);
    observer.observe(strip);
    update();
    return () => {
      strip.removeEventListener('scroll', update);
      observer.disconnect();
    };
  }, [images]);
  const scroll = (direction: number) =>
    stripRef.current?.scrollBy({
      left: direction * stripRef.current.clientWidth * 0.75,
      behavior: window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'instant' : 'smooth',
    });

  return (
    <section aria-label="Photos in this survey" className="min-w-0">
      <div role="group" aria-label="Show photos" className="mb-3 flex flex-wrap gap-2">
        {FILTERS.map((f) => (
          <Button
            key={f.value}
            type="button"
            variant={filter === f.value ? 'primary' : 'outline'}
            aria-pressed={filter === f.value}
            onClick={() => onFilter(f.value)}
            className="min-h-11 rounded-full px-4! text-xs!"
          >
            {f.label}
          </Button>
        ))}
        {loading && <span className="self-center text-xs text-gray-500">Loading…</span>}
      </div>

      {images.length === 0 ? (
        <p className="py-6 text-center text-sm text-gray-500 dark:text-gray-400">No photos match this filter.</p>
      ) : (
        <Card bodyClassName="flex min-w-0 items-center gap-2 p-2!" className="border-gray-200 dark:border-gray-700">
          <Button
            variant="ghost"
            className="size-11 shrink-0 p-0!"
            aria-label="Scroll photos left"
            disabled={!scrollable.left}
            onClick={() => scroll(-1)}
            icon={<ChevronLeft size={18} aria-hidden="true" />}
          />
          <ol ref={stripRef} className="relative flex min-w-0 flex-1 gap-2 overflow-x-auto py-1">
            {images.map((img, i) => {
              const current = img.id === currentId;
              const status = [
                img.reviewed ? 'checked' : 'not checked',
                img.confirmed ? `${img.confirmed} pig${img.confirmed === 1 ? '' : 's'}` : null,
                img.pending ? `${img.pending} AI to check` : null,
                img.flagged ? 'flagged' : null,
              ]
                .filter(Boolean)
                .join(', ');
              return (
                <li key={img.id} className="shrink-0">
                  <button
                    ref={current ? currentRef : undefined}
                    type="button"
                    onClick={() => onOpen(img.id)}
                    aria-current={current ? 'true' : undefined}
                    aria-label={`Photo ${i + 1}: ${img.fileName}, ${status}`}
                    title={img.fileName}
                    className={cx(
                      'relative block h-[72px] w-24 overflow-hidden rounded-md border-2 bg-gray-200 focus:outline-none focus-visible:ring-focus dark:bg-gray-700',
                      current ? 'border-brand-600' : 'border-transparent hover:border-gray-400',
                    )}
                  >
                    <img
                      src={wfpApi.surveyThumbnailUrl(img.id)}
                      alt=""
                      loading="lazy"
                      className={cx('size-full object-cover', img.reviewed && !current && 'opacity-60')}
                    />
                    <span className="absolute left-1 top-1 flex gap-1">
                      {img.reviewed && (
                        <CheckCircle2 className="size-4 rounded-full bg-white text-green-600" aria-hidden="true" />
                      )}
                      {img.flagged && (
                        <Flag className="size-4 rounded bg-white p-0.5 text-red-600" aria-hidden="true" />
                      )}
                    </span>
                    <span className="absolute bottom-1 right-1 flex gap-1 text-[10px] font-bold leading-none">
                      {img.pending > 0 && (
                        <span className="rounded bg-amber-400 px-1 py-0.5 text-gray-900">{img.pending}?</span>
                      )}
                      {img.confirmed > 0 && (
                        <span className="rounded bg-green-600 px-1 py-0.5 text-white">{img.confirmed}</span>
                      )}
                    </span>
                  </button>
                </li>
              );
            })}
          </ol>
          <Button
            variant="ghost"
            className="size-11 shrink-0 p-0!"
            aria-label="Scroll photos right"
            disabled={!scrollable.right}
            onClick={() => scroll(1)}
            icon={<ChevronRight size={18} aria-hidden="true" />}
          />
        </Card>
      )}
    </section>
  );
}
