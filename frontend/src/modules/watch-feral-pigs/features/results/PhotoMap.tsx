import { useEffect, useRef, useState } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import type { PhotoPoint } from '../../api/types';
import { ntTime } from '@shared/lib/format';

/** Reads a theme colour token (Tailwind v4 exposes them as CSS variables); falls back when not defined. */
function token(name: string, fallback: string) {
  if (typeof window === 'undefined') return fallback;
  const v = getComputedStyle(document.documentElement).getPropertyValue(name).trim();
  return v || fallback;
}

const escapeHtml = (s: string) => s.replace(/[&<>"']/g, (ch) => `&#${ch.charCodeAt(0)};`);

/**
 * Photo locations from EXIF GPS on a Leaflet map. Bigger, coloured dots = more pigs confirmed; small grey dots = no pigs.
 * A dashed line joins photos with timestamps in capture order. Map tiles need internet; the dots
 * still draw without it.
 */
export function PhotoMap({
  photos,
  selectedPhoto,
  onSelectPhoto,
  immersive = false,
}: {
  photos: PhotoPoint[];
  selectedPhoto?: string;
  onSelectPhoto?: (id: string) => void;
  immersive?: boolean;
}) {
  const el = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markers = useRef(new Map<string, L.CircleMarker>());
  const [tilesUnavailable, setTilesUnavailable] = useState(false);

  useEffect(() => {
    const located = photos.filter((p) => p.latitude != null && p.longitude != null);
    if (!el.current || located.length === 0) return;

    const map = L.map(el.current, {
      scrollWheelZoom: true,
      touchZoom: true,
      zoomSnap: 0.25,
      zoomDelta: 0.5,
      zoomControl: false,
    });
    mapRef.current = map;
    const markerIndex = new Map<string, L.CircleMarker>();
    markers.current = markerIndex;
    L.control.zoom({ position: 'topright' }).addTo(map);
    const street = L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      maxZoom: 19,
      maxNativeZoom: 19,
      attribution: '&copy; OpenStreetMap contributors',
    });
    // Esri has no sharper imagery than zoom 17 in most of rural NT and answers "Map data not yet available" above it.
    // maxNativeZoom makes Leaflet enlarge the zoom-17 tiles instead of asking for ones that don't exist.
    const satellite = L.tileLayer(
      'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
      {
        maxZoom: 19,
        maxNativeZoom: 17,
        attribution: 'Tiles &copy; Esri',
      },
    );
    satellite.addTo(map);
    const tileError = () => setTilesUnavailable(true);
    const tileLoaded = () => setTilesUnavailable(false);
    street.on('tileerror', tileError).on('tileload', tileLoaded);
    satellite.on('tileerror', tileError).on('tileload', tileLoaded);
    L.control.layers({ Satellite: satellite, Street: street }, undefined, { position: 'topright' }).addTo(map);

    const pigColour = token('--color-orange-500', '#f97316');
    const noneColour = token('--color-gray-400', '#9ca3af');
    const pathColour = token('--color-gray-100', '#f3f4f6');

    const ordered = [...located].sort((a, b) => (a.takenAt ?? '').localeCompare(b.takenAt ?? ''));
    const timed = ordered.filter((p) => p.takenAt != null);
    if (timed.length > 1) {
      L.polyline(
        timed.map((p) => [p.latitude!, p.longitude!] as [number, number]),
        {
          color: pathColour,
          weight: 1.5,
          dashArray: '4 4',
          opacity: 0.8,
        },
      ).addTo(map);
    }

    // Draw photos without pigs first so the pig dots sit on top.
    for (const p of [...ordered].sort((a, b) => a.confirmed - b.confirmed)) {
      const hasPigs = p.confirmed > 0;
      const marker = L.circleMarker([p.latitude!, p.longitude!], {
        radius: hasPigs ? 6 + Math.min(p.confirmed, 10) : 4,
        color: hasPigs ? token('--color-white', '#fff') : noneColour,
        weight: hasPigs ? 2 : 1,
        fillColor: hasPigs ? pigColour : noneColour,
        fillOpacity: hasPigs ? 0.9 : 0.6,
      }).addTo(map);
      const when = p.takenAt ? ` · ${ntTime(p.takenAt)}` : '';
      marker.bindTooltip(
        `<strong>${p.confirmed} pig${p.confirmed === 1 ? '' : 's'}</strong><br/>${escapeHtml(p.fileName)}${when}` +
          (p.reviewed ? '' : '<br/><em>not checked yet</em>'),
      );
      marker.on('click', () => onSelectPhoto?.(p.imageId));
      // SVG paths are not keyboard-focusable by default. The same sheet opens on Enter/Space.
      const enableKeyboard = () => {
        const path = marker.getElement();
        path?.setAttribute('tabindex', '0');
        path?.setAttribute('role', 'button');
        path?.setAttribute('aria-label', `Open photo ${p.fileName}, ${p.confirmed} confirmed sightings`);
        path?.addEventListener('keydown', (event) => {
          const key = (event as KeyboardEvent).key;
          if (key === 'Enter' || key === ' ') {
            event.preventDefault();
            onSelectPhoto?.(p.imageId);
          }
        });
      };
      marker.on('add', enableKeyboard);
      if (marker.getElement()) enableKeyboard();
      markerIndex.set(p.imageId, marker);
    }

    const bounds = L.latLngBounds(ordered.map((p) => [p.latitude!, p.longitude!] as [number, number]));
    const resize = () => {
      map.invalidateSize({ pan: false });
      const wide = immersive && window.matchMedia('(min-width: 1280px) and (min-height: 800px)').matches;
      map.fitBounds(bounds.pad(0.2), {
        maxZoom: 17,
        paddingTopLeft: wide ? [390, 60] : [30, 30],
        paddingBottomRight: wide ? [70, 100] : [65, 90],
        animate: false,
      });
    };
    const observer = new ResizeObserver(resize);
    observer.observe(el.current);
    resize();

    return () => {
      observer.disconnect();
      markerIndex.clear();
      mapRef.current = null;
      map.remove();
    };
  }, [photos, immersive, onSelectPhoto]);

  useEffect(() => {
    const marker = selectedPhoto ? markers.current.get(selectedPhoto) : undefined;
    const map = mapRef.current;
    if (!marker || !map || !el.current) return;
    const wide = immersive && window.matchMedia('(min-width: 1280px) and (min-height: 800px)').matches;
    const size = map.getSize();
    // Selected points stay above the photo sheet, which can occupy the lower half.
    const target = wide
      ? L.point((390 + size.x - 70) / 2, (60 + size.y - 500) / 2)
      : L.point(size.x / 2, Math.min(size.y / 4, 120));
    const centre = map.project(marker.getLatLng()).subtract(target.subtract(size.divideBy(2)));
    map.panTo(map.unproject(centre), { animate: false });
    marker.setStyle({ weight: 4 });
    for (const [imageId, other] of markers.current) {
      if (imageId !== selectedPhoto) other.setStyle({ weight: other.options.radius! > 4 ? 2 : 1 });
    }
    if (!wide) el.current.scrollIntoView({ block: 'start', behavior: 'instant' });
  }, [selectedPhoto, immersive, photos]);

  return (
    <div className={immersive ? 'relative h-full min-h-0 bg-gray-200 dark:bg-gray-700' : ''}>
      <div
        ref={el}
        role="region"
        aria-label="Map of photo locations, sized by number of pigs confirmed"
        className={
          immersive
            ? 'z-0 h-full w-full overflow-hidden'
            : 'z-0 h-80 w-full overflow-hidden rounded-lg border border-gray-200 md:h-[28rem] dark:border-gray-700'
        }
      />
      {tilesUnavailable && (
        <p
          role="status"
          className="absolute right-16 top-4 z-10 max-w-52 rounded bg-white p-3 text-xs text-gray-700 shadow dark:bg-gray-800 dark:text-gray-200"
        >
          Basemap unavailable. Photo locations still work; try Street in map layers.
        </p>
      )}
      <p
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
    </div>
  );
}
