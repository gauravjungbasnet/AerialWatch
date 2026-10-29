import { readFile } from 'node:fs/promises';
import assert from 'node:assert/strict';
import test from 'node:test';
import ts from 'typescript';
const load = async (name) => {
  const source = await readFile(
    new URL(
      name === 'drawGesture'
        ? '../src/shared/imaging/viewer/gestures.ts'
        : `../src/modules/watch-feral-pigs/features/review/${name}.ts`,
      import.meta.url,
    ),
    'utf8',
  );
  const { outputText } = ts.transpileModule(source, {
    compilerOptions: { module: ts.ModuleKind.ESNext, target: ts.ScriptTarget.ES2020 },
  });
  return import(`data:text/javascript;base64,${Buffer.from(outputText).toString('base64')}`);
};
const { DrawingHistory } = await load('drawingHistory');
const { drawGestureRequested } = await load('drawGesture');
const rect = { x: 20, y: 30, width: 40, height: 50 };
function port() {
  let n = 0;
  const boxes = [];
  return {
    boxes,
    add: async (imageId, r) => {
      const b = { ...r, id: `r${++n}`, imageId, source: 'ranger', status: 'accepted', label: 'pig' };
      boxes.push(b);
      return b;
    },
    remove: async (id) => {
      boxes.splice(
        boxes.findIndex((b) => b.id === id),
        1,
      );
    },
  };
}
test('Ctrl/Cmd temporarily draw; normal selection and read-only twin remain unchanged', () => {
  assert.equal(drawGestureRequested('select', true, true, false), true);
  assert.equal(drawGestureRequested('select', true, false, true), true);
  assert.equal(drawGestureRequested('draw', true, false, false), true);
  assert.equal(drawGestureRequested('select', true, false, false), false);
  assert.equal(drawGestureRequested('draw', false, true, true), false);
});
test('undo restores moved geometry on redo with a new server id', async () => {
  const h = new DrawingHistory(),
    p = port();
  const first = await h.execute('photo', 'draw', p, p.boxes, rect);
  p.boxes[0].x = 99;
  await h.execute('photo', 'undo', p, p.boxes);
  assert.equal(p.boxes.length, 0);
  const redo = await h.execute('photo', 'redo', p, p.boxes);
  assert.equal(redo.x, 99);
  assert.notEqual(redo.id, first.id);
  await h.execute('photo', 'undo', p, p.boxes);
  assert.equal(p.boxes.length, 0);
});
test('photo histories are separate; new drawings clear redo only for their photo', async () => {
  const h = new DrawingHistory(),
    p = port();
  await h.execute('a', 'draw', p, p.boxes, rect);
  await h.execute('a', 'undo', p, p.boxes);
  await h.execute('b', 'draw', p, p.boxes, rect);
  assert.equal(h.canRedo('a'), true);
  assert.equal(h.canRedo('b'), false);
  await h.execute('a', 'draw', p, p.boxes, rect);
  assert.equal(h.canRedo('a'), false);
});
test('failed saves and deletes preserve retryable history', async () => {
  const h = new DrawingHistory(),
    p = port();
  await h.execute('a', 'draw', p, p.boxes, rect);
  const bad = {
    ...p,
    remove: async () => {
      throw Error('offline');
    },
  };
  await assert.rejects(h.execute('a', 'undo', bad, p.boxes));
  assert.equal(h.canUndo('a', p.boxes), true);
  assert.equal(h.canRedo('a'), false);
  await h.execute('a', 'undo', p, p.boxes);
  await assert.rejects(
    h.execute(
      'a',
      'redo',
      {
        ...p,
        add: async () => {
          throw Error('offline');
        },
      },
      p.boxes,
    ),
  );
  assert.equal(h.canRedo('a'), true);
  assert.equal(h.busy, false);
});
test('undo skips explicitly removed boxes and never deletes an AI suggestion', async () => {
  const h = new DrawingHistory(),
    p = port();
  await h.execute('a', 'draw', p, p.boxes, rect);
  await h.execute('a', 'draw', p, p.boxes, rect);
  p.boxes.pop();
  p.boxes.push({ ...rect, id: 'ai', source: 'ai' });
  await h.execute('a', 'undo', p, p.boxes);
  assert.deepEqual(
    p.boxes.map((b) => b.id),
    ['ai'],
  );
  assert.equal(h.canUndo('a', p.boxes), false);
});
test('rapid commands cannot create concurrent writes', async () => {
  const h = new DrawingHistory(),
    p = port();
  let release;
  const pending = new Promise((resolve) => {
    release = resolve;
  });
  const first = h.execute(
    'a',
    'draw',
    {
      ...p,
      add: async (...args) => {
        await pending;
        return p.add(...args);
      },
    },
    p.boxes,
    rect,
  );
  assert.equal(await h.execute('a', 'draw', p, p.boxes, rect), undefined);
  release();
  await first;
  assert.equal(p.boxes.length, 1);
});
