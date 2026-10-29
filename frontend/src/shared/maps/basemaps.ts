import L from 'leaflet';

/** Tile sources shared by every map. Tiles need internet; markers and lines still draw without them. */
const BASEMAPS = [
  {
    name: 'Satellite',
    url: 'https://server.arcgisonline.com/ArcGIS/rest/services/World_Imagery/MapServer/tile/{z}/{y}/{x}',
    attribution: 'Tiles &copy; Esri',
    // Esri has no sharper imagery than zoom 17 in most of rural NT and answers "Map data not yet available" above it.
    // maxNativeZoom makes Leaflet enlarge the zoom-17 tiles instead of asking for ones that don't exist.
    maxNativeZoom: 17,
  },
  {
    name: 'Street',
    url: 'https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png',
    attribution: '&copy; OpenStreetMap contributors',
    maxNativeZoom: 19,
  },
];
const MAX_ZOOM = 19;

/** Adds the zoom buttons, the basemaps (satellite first) and the layer switcher; reports whether tiles are loading. */
export function addBasemaps(map: L.Map, onTiles: (available: boolean) => void) {
  L.control.zoom({ position: 'topright' }).addTo(map);
  const layers = BASEMAPS.map((b) =>
    L.tileLayer(b.url, { maxZoom: MAX_ZOOM, maxNativeZoom: b.maxNativeZoom, attribution: b.attribution })
      .on('tileerror', () => onTiles(false))
      .on('tileload', () => onTiles(true)),
  );
  layers[0].addTo(map);
  L.control
    .layers(Object.fromEntries(BASEMAPS.map((b, i) => [b.name, layers[i]])), undefined, { position: 'topright' })
    .addTo(map);
}
