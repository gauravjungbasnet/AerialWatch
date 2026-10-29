/**
 * Neutral map contracts (docs/18 step 5). No module or Leaflet imports: a module turns its own records into these in
 * an adapter, and `MapView` draws them.
 */

/** A circle marker. Later points in the list draw on top of earlier ones. */
export interface MapPoint {
  id: string;
  lat: number;
  lng: number;
  /** Radius in screen pixels. */
  radius: number;
  /** CSS colours for the outline and the fill. */
  stroke: string;
  fill: string;
  /** Outline width in screen pixels; the selected point is drawn thicker. */
  weight: number;
  fillOpacity: number;
  /** Plain text (escaped by the map): a bold first line, further lines, and an optional italic note. */
  tooltip: { title: string; lines?: string[]; note?: string };
  /** Accessible name of the point's button, e.g. "Open photo X, 2 confirmed sightings". */
  label: string;
}

/** A dashed line joining points in order (e.g. capture order). It is not a recorded route. */
export interface MapPath {
  latLngs: [number, number][];
  color: string;
}

/** Screen pixels to keep clear on each side of the map. */
export interface MapPadding {
  top: number;
  right: number;
  bottom: number;
  left: number;
}
