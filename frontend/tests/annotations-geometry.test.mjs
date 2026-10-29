import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
import test from 'node:test';
import ts from 'typescript';
const src = await readFile(new URL('../src/shared/imaging/annotations/geometry.ts', import.meta.url), 'utf8');
const js = ts.transpileModule(src.replace("import type { ChangeKind, Rect } from './types';", ''), {
  compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2020 },
}).outputText;
const g = await import(`data:text/javascript;base64,${Buffer.from(js).toString('base64')}`);
const r = (x, y, width, height) => ({ x, y, width, height });
test('rectFromPoints works in any drag direction and stays inside the image', () => {
  assert.deepEqual(g.rectFromPoints({ x: 50, y: 60 }, { x: 10, y: 20 }, 100, 100), r(10, 20, 40, 40));
  assert.deepEqual(g.rectFromPoints({ x: 90, y: 90 }, { x: 150, y: 150 }, 100, 100), r(90, 90, 10, 10));
});
test('changeKind separates a move from a resize', () => {
  assert.equal(g.changeKind(r(0, 0, 10, 10), r(5, 5, 10, 10)), 'move');
  assert.equal(g.changeKind(r(0, 0, 10, 10), r(0, 0, 12, 10)), 'resize');
});
test('followResize ignores moves and scales resizes by the size ratio', () => {
  assert.equal(g.followResize(r(0, 0, 10, 10), r(5, 5, 10, 10), r(0, 0, 20, 20)), null);
  assert.deepEqual(g.followResize(r(10, 10, 10, 10), r(10, 10, 20, 10), r(100, 100, 20, 20)), r(100, 100, 40, 20));
});
test('followResize keeps the partner inside its image and at least 4 px', () => {
  assert.deepEqual(
    g.followResize(r(0, 0, 10, 10), r(0, 0, 20, 10), r(90, 0, 10, 10), { w: 100, h: 100 }),
    r(80, 0, 20, 10),
  );
  assert.equal(g.followResize(r(0, 0, 100, 100), r(0, 0, 1, 1), r(0, 0, 10, 10)).width, 4);
});
test('isBigEnough needs both sides at the minimum', () => {
  assert.equal(g.isBigEnough(r(0, 0, 8, 8), 8), true);
  assert.equal(g.isBigEnough(r(0, 0, 8, 7), 8), false);
});
