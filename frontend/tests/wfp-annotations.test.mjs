import assert from 'node:assert/strict';
import test from 'node:test';
import { loadTs } from './load-ts.mjs';

const a = loadTs('modules/watch-feral-pigs/adapters/annotations.ts');

const box = (id, status, source = 'ai', confidence = 0.87) => ({
  id,
  imageId: 'p1',
  x: 10,
  y: 20,
  width: 30,
  height: 40,
  label: 'pig',
  source,
  status,
  confidence,
});
const mark = (detectionId, number, state, rect = { x: 1, y: 2, width: 3, height: 4 }) => ({
  detectionId,
  number,
  state,
  ...rect,
});
const sighting = (markId, label, state, counted = { x: 5, y: 6, width: 7, height: 8 }) => ({
  markId,
  label,
  state,
  twinX: 50,
  twinY: 60,
  twinWidth: 70,
  twinHeight: 80,
  ...counted,
});

test('counted photo: every box, numbered like the box list, styled by state', () => {
  const out = a.countedBoxAnnotations([box('d1', 'pending'), box('d2', 'accepted'), box('d3', 'accepted', 'ranger')]);
  assert.deepEqual(
    out.map((x) => x.id),
    ['d1', 'd2', 'd3'],
  );
  assert.deepEqual(
    out.map((x) => x.label),
    ['1 · AI 87%', '2 · ✓ AI 87%', '3 · ✓ added'],
  );
  assert.deepEqual(out[0].appearance.dash, [6, 4]); // pending = dashed, never colour alone
  assert.equal(out[1].appearance.dash, undefined);
  assert.deepEqual(
    { x: out[0].x, y: out[0].y, width: out[0].width, height: out[0].height },
    { x: 10, y: 20, width: 30, height: 40 },
  );
});

test('sighting ids round-trip and only still-suggested sightings appear on the counted photo', () => {
  assert.equal(a.markIdOfSighting(a.sightingId('m7')), 'm7');
  assert.equal(a.markIdOfSighting('d1'), null);
  const out = a.sightingSuggestionAnnotations(
    [
      sighting('m1', 'c1', 'suggestedOnCounted'),
      sighting('m2', 'c2', 'notVisibleOnCounted'),
      sighting('m3', 'c3', 'suggestedOnCounted', { x: undefined }),
    ],
    'colour (rgb)',
  );
  assert.deepEqual(
    out.map((x) => [x.id, x.label]),
    [['sighting:m1', 'c1 · from colour (rgb)']],
  );
  assert.deepEqual([out[0].x, out[0].width], [5, 7]); // position suggested on the counted photo
});

test('twin pane: placed and suggested pigs share the detection id; not-visible pigs are not drawn', () => {
  const out = a.twinPaneAnnotations(
    [mark('d1', 1, 'confirmed'), mark('d2', 2, 'suggested'), mark('d3', 3, 'notVisible')],
    [sighting('m1', 'c1', 'suggestedOnCounted')],
  );
  assert.deepEqual(
    out.map((x) => [x.id, x.label]),
    [
      ['d1', '1 · ✓ placed'],
      ['d2', '2 · suggested'],
      ['sighting:m1', 'c1 · drawn here'],
    ],
  );
  assert.equal(out[0].appearance.dash, undefined);
  assert.deepEqual(out[1].appearance.dash, [6, 4]);
  assert.deepEqual([out[2].x, out[2].width], [50, 70]); // drawn position on the twin photo
});

test('results: only confirmed pigs, with review numbering; twin shows placed pigs and pigs seen only there', () => {
  const counted = a.confirmedBoxAnnotations([box('d1', 'rejected'), box('d2', 'accepted'), box('d3', 'pending')]);
  assert.deepEqual(
    counted.map((x) => [x.id, x.label]),
    [['d2', '2']],
  );
  assert.deepEqual(a.twinEvidenceAnnotations(undefined), []);
  const twin = a.twinEvidenceAnnotations({
    imageId: 'p1',
    learnedFrom: 0,
    marks: [mark('d2', 2, 'confirmed'), mark('d4', 4, 'suggested')],
    sightings: [sighting('m1', 'c1', 'notVisibleOnCounted'), sighting('m2', 'c2', 'suggestedOnCounted')],
  });
  assert.deepEqual(
    twin.map((x) => [x.id, x.label]),
    [
      ['d2', '2'],
      ['sighting:m1', 'c1'],
    ],
  );
  assert.deepEqual(twin[1].appearance.dash, [6, 4]);
});
