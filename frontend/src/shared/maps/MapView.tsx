import { useEffect, useRef, useState, type ReactNode } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';
import { addBasemaps } from './basemaps';
import { coveredPadding, withMargin } from './padding';
import type { MapPadding, MapPath, MapPoint } from './types';

interface MapViewProps {
  points: MapPoint[];
  path?: MapPath;
  selectedId?: string;
  onSelect?(id: string): void;
  /**
   * Elements floating over the map (panels, sheets, legends). Fitting and focusing keep points clear of them; their
   * sizes are measured, so the layout can change without touching the map.
   */
  overlays?: (HTMLElement | null)[];
  /** Accessible name of the map region. */
  label: string;
  /** Wrapper (positions the overlays passed as children) and map element classes. */
  className?: string;
  mapClassName?: string;
  tilesUnavailableText?: string;
  children?: ReactNode;
}

const MARGIN = 24;
const NO_OVERLAYS: (HTMLElement | null)[] = [];
const escapeHtml = (s: string) => s.replace(/[&<>"']/g, (ch) => `&#${ch.charCodeAt(0)};`);
const tooltipHtml = ({ title, lines = [], note }: MapPoint['tooltip']) =>
  [
    `<strong>${escapeHtml(title)}</strong>`,
    ...lines.map(escapeHtml),
    ...(note ? [`<em>${escapeHtml(note)}</em>`] : []),
  ].join('<br/>');

/**
 * Reusable Leaflet map (docs/18 step 5): basemaps, zoom, circle markers with keyboard access, an optional dashed path,
 * selection, and fitting around floating panels. It knows nothing about any module; callers pass `MapPoint`s.
 */
export function MapView({
  points,
  path,
  selectedId,
  onSelect,
  overlays = NO_OVERLAYS,
  label,
  className,
  mapClassName,
  tilesUnavailableText = 'Basemap unavailable. The markers still work; try Street in map layers.',
  children,
}: MapViewProps) {
  const el = useRef<HTMLDivElement>(null);
  const mapRef = useRef<L.Map | null>(null);
  const markers = useRef(new Map<string, { marker: L.CircleMarker; weight: number }>());
  const [tilesUnavailable, setTilesUnavailable] = useState(false);
  const latest = useRef({ onSelect, overlays });
  latest.current = { onSelect, overlays };

  /** Pixels to keep clear: Leaflet's own controls, the overlays and a margin, measured now. */
  const clearance = (): MapPadding => {
    const map = mapRef.current;
    const container = el.current;
    if (!map || !container) return { top: MARGIN, right: MARGIN, bottom: MARGIN, left: MARGIN };
    const controls = Array.from(container.querySelectorAll<HTMLElement>('.leaflet-top, .leaflet-bottom'));
    const boxes = [...controls, ...latest.current.overlays]
      .filter((o): o is HTMLElement => o != null)
      .map((o) => o.getBoundingClientRect());
    return withMargin(coveredPadding(container.getBoundingClientRect(), boxes), MARGIN, map.getSize());
  };

  /** Pans so the selected point sits in the middle of the area no panel covers. */
  const focus = (id: string | undefined) => {
    const entry = id ? markers.current.get(id) : undefined;
    const map = mapRef.current;
    if (!entry || !map) return;
    const pad = clearance();
    const size = map.getSize();
    const target = L.point((pad.left + size.x - pad.right) / 2, (pad.top + size.y - pad.bottom) / 2);
    const centre = map.project(entry.marker.getLatLng()).subtract(target.subtract(size.divideBy(2)));
    map.panTo(map.unproject(centre), { animate: false });
  };
  const focusRef = useRef(focus);
  focusRef.current = focus;
  const selectedRef = useRef(selectedId);
  selectedRef.current = selectedId;
  const fitRef = useRef<() => void>(() => {});
  // The same elements in a new array are the same overlays (callers usually pass an inline array).
  const overlayList = useRef(overlays);
  if (overlays.length !== overlayList.current.length || overlays.some((o, i) => o !== overlayList.current[i]))
    overlayList.current = overlays;
  const currentOverlays = overlayList.current;

  // Build the map once per set of points.
  useEffect(() => {
    if (!el.current || points.length === 0) return;
    const map = L.map(el.current, {
      scrollWheelZoom: true,
      touchZoom: true,
      zoomSnap: 0.25,
      zoomDelta: 0.5,
      zoomControl: false,
    });
    mapRef.current = map;
    const index = new Map<string, { marker: L.CircleMarker; weight: number }>();
    markers.current = index;
    addBasemaps(map, (available) => setTilesUnavailable(!available));

    if (path && path.latLngs.length > 1)
      L.polyline(path.latLngs, { color: path.color, weight: 1.5, dashArray: '4 4', opacity: 0.8 }).addTo(map);

    for (const p of points) {
      const marker = L.circleMarker([p.lat, p.lng], {
        radius: p.radius,
        color: p.stroke,
        weight: p.weight,
        fillColor: p.fill,
        fillOpacity: p.fillOpacity,
      }).addTo(map);
      marker.bindTooltip(tooltipHtml(p.tooltip));
      marker.on('click', () => latest.current.onSelect?.(p.id));
      // SVG paths are not keyboard-focusable by default; Enter/Space selects like a click.
      const enableKeyboard = () => {
        const node = marker.getElement();
        node?.setAttribute('tabindex', '0');
        node?.setAttribute('role', 'button');
        node?.setAttribute('aria-label', p.label);
        node?.addEventListener('keydown', (event) => {
          const key = (event as KeyboardEvent).key;
          if (key === 'Enter' || key === ' ') {
            event.preventDefault();
            latest.current.onSelect?.(p.id);
          }
        });
      };
      marker.on('add', enableKeyboard);
      if (marker.getElement()) enableKeyboard();
      index.set(p.id, { marker, weight: p.weight });
    }

    const bounds = L.latLngBounds(points.map((p) => [p.lat, p.lng] as [number, number]));
    const fit = () => {
      map.invalidateSize({ pan: false });
      const pad = clearance();
      map.fitBounds(bounds.pad(0.2), {
        maxZoom: 17,
        paddingTopLeft: [pad.left, pad.top],
        paddingBottomRight: [pad.right, pad.bottom],
        animate: false,
      });
    };
    fitRef.current = fit;
    const observer = new ResizeObserver(fit);
    observer.observe(el.current);
    fit();

    return () => {
      observer.disconnect();
      index.clear();
      fitRef.current = () => {};
      mapRef.current = null;
      map.remove();
    };
  }, [points, path]);

  // Selection: thicker outline, bring the map into view if it was scrolled away, centre the point in the free area.
  useEffect(() => {
    for (const [id, { marker, weight }] of markers.current) marker.setStyle({ weight: id === selectedId ? 4 : weight });
    const container = el.current;
    if (!selectedId || !container || !markers.current.has(selectedId)) return;
    const r = container.getBoundingClientRect();
    if (r.top < 0 || r.bottom > window.innerHeight + 1)
      container.scrollIntoView({ block: 'start', behavior: 'instant' });
    focusRef.current(selectedId);
  }, [selectedId, points]);

  // Panels appear or change size (a sheet opens, a photo loads): keep the selected point, or else all points, clear.
  useEffect(() => {
    const present = currentOverlays.filter((o): o is HTMLElement => o != null);
    if (present.length === 0) return;
    const observer = new ResizeObserver(() =>
      selectedRef.current ? focusRef.current(selectedRef.current) : fitRef.current(),
    );
    present.forEach((o) => observer.observe(o));
    return () => observer.disconnect();
  }, [currentOverlays]);

  return (
    <div className={className}>
      <div ref={el} role="region" aria-label={label} className={mapClassName} />
      {tilesUnavailable && (
        <p
          role="status"
          className="absolute right-16 top-4 z-10 max-w-52 rounded bg-white p-3 text-xs text-gray-700 shadow dark:bg-gray-800 dark:text-gray-200"
        >
          {tilesUnavailableText}
        </p>
      )}
      {children}
    </div>
  );
}
