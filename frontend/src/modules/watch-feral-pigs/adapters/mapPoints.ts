import type { MapPath, MapPoint } from '@shared/maps/types';
import { ntTime } from '@shared/lib/dates';
import type { PhotoPoint } from '../api/types';

/** Colours for the photo map; resolved from theme tokens by the caller so this stays a pure function. */
export interface PhotoMapPalette {
  pigs: string;
  none: string;
  outline: string;
  path: string;
}

/**
 * WFP photo locations → the shared map's points (docs/18 step 5). Bigger orange dots = more pigs confirmed; small grey
 * dots = none. Photos without pigs come first so pig dots draw on top. The dashed path joins photos with a timestamp in
 * capture order; it is not a recorded flight path.
 */
export function photoMapLayers(photos: PhotoPoint[], palette: PhotoMapPalette): { points: MapPoint[]; path: MapPath } {
  const located = photos
    .flatMap((p) => (p.latitude != null && p.longitude != null ? [{ ...p, lat: p.latitude, lng: p.longitude }] : []))
    .sort((a, b) => (a.takenAt ?? '').localeCompare(b.takenAt ?? ''));
  const path: MapPath = {
    latLngs: located.filter((p) => p.takenAt != null).map((p) => [p.lat, p.lng]),
    color: palette.path,
  };
  const points = [...located]
    .sort((a, b) => a.confirmed - b.confirmed)
    .map((p): MapPoint => {
      const hasPigs = p.confirmed > 0;
      return {
        id: p.imageId,
        lat: p.lat,
        lng: p.lng,
        radius: hasPigs ? 6 + Math.min(p.confirmed, 10) : 4,
        stroke: hasPigs ? palette.outline : palette.none,
        weight: hasPigs ? 2 : 1,
        fill: hasPigs ? palette.pigs : palette.none,
        fillOpacity: hasPigs ? 0.9 : 0.6,
        tooltip: {
          title: `${p.confirmed} pig${p.confirmed === 1 ? '' : 's'}`,
          lines: [`${p.fileName}${p.takenAt ? ` · ${ntTime(p.takenAt)}` : ''}`],
          note: p.reviewed ? undefined : 'not checked yet',
        },
        label: `Open photo ${p.fileName}, ${p.confirmed} confirmed sightings`,
      };
    });
  return { points, path };
}
