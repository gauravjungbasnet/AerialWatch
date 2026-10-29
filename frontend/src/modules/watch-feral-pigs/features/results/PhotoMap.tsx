import { useMemo, useState } from 'react';
import { MapView } from '@shared/maps/MapView';
import { themeColor } from '@shared/lib/theme';
import type { PhotoPoint } from '../../api/types';
import { photoMapLayers } from '../../adapters/mapPoints';

interface PhotoMapProps {
  photos: PhotoPoint[];
  selectedPhoto?: string;
  onSelectPhoto?: (id: string) => void;
  /** Full-height map behind floating panels (Results on a large screen); otherwise a card-sized map. */
  immersive?: boolean;
  /** Panels floating over the map; points are kept clear of them. */
  overlays?: (HTMLElement | null)[];
}

/**
 * Photo locations from EXIF GPS on the shared map (docs/18 step 5), with the WFP legend. Map tiles need internet; the
 * dots still draw without it.
 */
export function PhotoMap({ photos, selectedPhoto, onSelectPhoto, immersive = false, overlays = [] }: PhotoMapProps) {
  const [legend, setLegend] = useState<HTMLParagraphElement | null>(null);
  const { points, path } = useMemo(
    () =>
      photoMapLayers(photos, {
        pigs: themeColor('--color-orange-500', '#f97316'),
        none: themeColor('--color-gray-400', '#9ca3af'),
        outline: themeColor('--color-white', '#fff'),
        path: themeColor('--color-gray-100', '#f3f4f6'),
      }),
    [photos],
  );

  return (
    <MapView
      points={points}
      path={path}
      selectedId={selectedPhoto}
      onSelect={onSelectPhoto}
      overlays={immersive ? [legend, ...overlays] : overlays}
      label="Map of photo locations, sized by number of pigs confirmed"
      tilesUnavailableText="Basemap unavailable. Photo locations still work; try Street in map layers."
      className={immersive ? 'relative h-full min-h-0 bg-gray-200 dark:bg-gray-700' : ''}
      mapClassName={
        immersive
          ? 'z-0 h-full w-full overflow-hidden'
          : 'z-0 h-80 w-full overflow-hidden rounded-lg border border-gray-200 md:h-[28rem] dark:border-gray-700'
      }
    >
      <p
        ref={setLegend}
        className={
          immersive
            ? 'map-legend absolute bottom-6 left-3 z-10 flex max-w-72 flex-col items-start gap-1 rounded-lg bg-white p-3 text-xs text-gray-700 shadow dark:bg-gray-800 dark:text-gray-200'
            : 'mt-2 flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-gray-500 dark:text-gray-400'
        }
      >
        <span className="inline-flex items-center gap-1.5">
          <span
            className="inline-block size-3 shrink-0 rounded-full border-2 border-white bg-orange-500 shadow"
            aria-hidden="true"
          />
          Pigs confirmed (bigger = more)
        </span>
        <span className="inline-flex items-center gap-1.5">
          <span className="inline-flex size-3 shrink-0 items-center justify-center" aria-hidden="true">
            <span className="size-2 rounded-full bg-gray-400" />
          </span>
          None confirmed
        </span>
        <span className="mt-1 block">Scroll or pinch to zoom</span>
      </p>
    </MapView>
  );
}
