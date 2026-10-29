import assert from 'node:assert/strict';
import test from 'node:test';
import { loadTs } from './load-ts.mjs';

const { coveredPadding, withMargin } = loadTs('shared/maps/padding.ts');
const { photoMapLayers } = loadTs('modules/watch-feral-pigs/adapters/mapPoints.ts');

const map = { left: 0, top: 0, right: 1200, bottom: 900 };

test('padding: each panel counts against the side it covers least; panels off the map are ignored', () => {
  const summary = { left: 24, top: 24, right: 360, bottom: 700 }; // tall panel on the left
  const sheet = { left: 500, top: 400, right: 1176, bottom: 900 }; // sheet along the bottom
  const button = { left: 1000, top: 820, right: 1176, bottom: 870 }; // small button near the bottom
  const elsewhere = { left: 1300, top: 0, right: 1400, bottom: 100 };
  assert.deepEqual(coveredPadding(map, [summary, sheet, button, elsewhere]), {
    top: 0,
    right: 0,
    bottom: 500,
    left: 360,
  });
});

test('margin is added and at least a quarter of the map stays free', () => {
  assert.deepEqual(withMargin({ top: 0, right: 0, bottom: 0, left: 0 }, 24, { x: 1200, y: 900 }), {
    top: 24,
    right: 24,
    bottom: 24,
    left: 24,
  });
  const squeezed = withMargin({ top: 0, right: 0, bottom: 900, left: 0 }, 0, { x: 1200, y: 900 });
  assert.equal(squeezed.bottom, 675);
});

test('photo map: located photos only, pig dots on top, sized by count, path in capture order', () => {
  const palette = { pigs: 'orange', none: 'grey', outline: 'white', path: 'light' };
  const photo = (imageId, confirmed, takenAt, gps = true) => ({
    imageId,
    fileName: `${imageId}.JPG`,
    confirmed,
    takenAt,
    reviewed: confirmed > 0,
    flagged: false,
    ...(gps ? { latitude: -14 - confirmed / 100, longitude: 132 } : {}),
  });
  const { points, path } = photoMapLayers(
    [
      photo('b', 3, '2026-10-20T09:02:00Z'),
      photo('a', 0, '2026-10-20T09:01:00Z'),
      photo('c', 20, undefined),
      photo('x', 1, undefined, false),
    ],
    palette,
  );
  assert.deepEqual(
    points.map((p) => [p.id, p.radius, p.fill]),
    [
      ['a', 4, 'grey'],
      ['b', 9, 'orange'],
      ['c', 16, 'orange'],
    ],
  );
  assert.deepEqual(path.latLngs.length, 2); // only photos with a time, oldest first
  assert.equal(points[0].tooltip.note, 'not checked yet');
  assert.equal(points[1].tooltip.title, '3 pigs');
  assert.equal(points[1].label, 'Open photo b.JPG, 3 confirmed sightings');
});
