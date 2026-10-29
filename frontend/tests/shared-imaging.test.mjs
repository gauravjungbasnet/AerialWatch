import test from 'node:test';
import assert from 'node:assert/strict';
import { readFileSync, existsSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import ts from 'typescript';
import { JSDOM } from 'jsdom';
import React, { act } from 'react';
import { createRoot } from 'react-dom/client';

// Compile the real TSX modules in memory; keep the existing Node test runner.
const require = createRequire(import.meta.url),
  cache = new Map();
function load(path) {
  path = resolve(path);
  if (cache.has(path)) return cache.get(path).exports;
  const module = { exports: {} };
  cache.set(path, module);
  const output = ts.transpileModule(readFileSync(path, 'utf8'), {
    compilerOptions: {
      module: ts.ModuleKind.CommonJS,
      jsx: ts.JsxEmit.ReactJSX,
      target: ts.ScriptTarget.ES2020,
      esModuleInterop: true,
    },
  }).outputText;
  const localRequire = (name) => {
    if (!name.startsWith('.')) return require(name);
    const base = resolve(dirname(path), name);
    const target = [base + '.ts', base + '.tsx', resolve(base, 'index.ts')].find(existsSync);
    return load(target);
  };
  new Function('require', 'module', 'exports', output)(localRequire, module, module.exports);
  return module.exports;
}
const source = fileURLToPath(new URL('../src/shared/imaging/', import.meta.url));
const { ImageViewer } = load(resolve(source, 'viewer/ImageViewer.tsx'));
const { ImageComparison } = load(resolve(source, 'comparison/ImageComparison.tsx'));
const { resolveViewerOptions } = load(resolve(source, 'viewer/options.ts'));
const h = React.createElement;
const mark = {
  id: 'a',
  x: 100,
  y: 100,
  width: 100,
  height: 100,
  label: 'Sample rectangle',
  appearance: { color: 'var(--color-brand-400)' },
};
const editing = { draw: true, move: true, resize: true, selection: true, pan: true, zoom: true };
async function mount(props = {}) {
  const dom = new JSDOM('<div id="root"></div>', { pretendToBeVisual: true });
  globalThis.window = dom.window;
  globalThis.document = dom.window.document;
  globalThis.IS_REACT_ACT_ENVIRONMENT = true;
  globalThis.ResizeObserver = class {
    observe() {}
    disconnect() {}
  };
  dom.window.HTMLElement.prototype.setPointerCapture = () => {};
  Object.defineProperties(dom.window.HTMLElement.prototype, {
    clientWidth: { get: () => 1000 },
    clientHeight: { get: () => 500 },
  });
  const container = document.getElementById('root'),
    root = createRoot(container),
    ref = React.createRef();
  let current = { src: 'synthetic.svg', alt: 'Synthetic sample', annotations: [mark], ref, ...props };
  async function render(next = {}) {
    current = { ...current, ...next };
    await act(() => root.render(h(ImageViewer, current)));
  }
  async function loaded() {
    const img = container.querySelector('img');
    Object.defineProperties(img, { naturalWidth: { value: 1000 }, naturalHeight: { value: 500 } });
    await act(() => img.dispatchEvent(new dom.window.Event('load')));
  }
  const event = async (target, name, x = 108, y = 103, extra = {}) => {
    const e = new dom.window.MouseEvent(name, {
      bubbles: true,
      cancelable: true,
      clientX: x,
      clientY: y,
      button: 0,
      ...extra,
    });
    Object.defineProperty(e, 'pointerId', { value: 1 });
    await act(() => target.dispatchEvent(e));
    return e;
  };
  await render();
  await loaded();
  return {
    dom,
    container,
    ref,
    render,
    loaded,
    event,
    viewer: () => container.querySelector('[role="img"]'),
    close: async () => {
      dom.window.close();
    },
  };
}

test('configuration requires explicit editing opt-in and callbacks', () => {
  const callbacks = { create: true, change: true, select: true };
  assert.equal(resolveViewerOptions({}, callbacks).draw, false);
  assert.equal(resolveViewerOptions(editing, { create: false, change: false, select: false }).resize, false);
  assert.equal(resolveViewerOptions(editing, callbacks).draw, true);
});

test('read-only preset renders original-pixel markers, allows page scroll and blocks every write and zoom', async () => {
  const calls = [];
  const m = await mount({
    options: { labels: 'all' },
    mode: 'draw',
    onCreate: (x) => calls.push(x),
    onChangeBox: (x) => calls.push(x),
  });
  try {
    assert.equal(m.container.querySelector('svg rect').getAttribute('x'), '100');
    assert.equal(m.container.querySelector('svg text').textContent, mark.label);
    assert.equal(m.container.querySelector('[data-handle]'), null);
    const transform = m.container.querySelector('.origin-top-left').style.transform;
    const wheel = new m.dom.window.WheelEvent('wheel', { deltaY: -120, cancelable: true });
    await act(() => m.viewer().dispatchEvent(wheel));
    assert.equal(wheel.defaultPrevented, false);
    await act(() => m.ref.current.zoomBy(2));
    assert.equal(m.container.querySelector('.origin-top-left').style.transform, transform);
    await m.event(m.viewer(), 'pointerdown', 108, 103, { ctrlKey: true });
    await m.event(m.viewer(), 'pointermove', 206, 201);
    await m.event(m.viewer(), 'pointerup');
    assert.deepEqual(calls, []);
  } finally {
    await m.close();
  }
});

test('editing preset supports modifier draw, move, resize and wheel zoom in image pixels', async () => {
  const creates = [],
    changes = [];
  const m = await mount({
    options: editing,
    selectedId: 'a',
    onSelect: () => {},
    onCreate: (r) => creates.push(r),
    onChangeBox: (id, r) => changes.push({ id, ...r }),
  });
  try {
    await m.event(m.viewer(), 'pointerdown', 108, 103, { ctrlKey: true });
    await m.event(m.viewer(), 'pointermove', 206, 201);
    await m.event(m.viewer(), 'pointerup');
    assert.deepEqual(creates, [{ x: 100, y: 100, width: 100, height: 100 }]);
    const box = m.container.querySelector('[data-box-id] rect');
    await m.event(box, 'pointerdown', 108, 103);
    await m.event(m.viewer(), 'pointermove', 157, 152);
    await m.event(m.viewer(), 'pointerup');
    assert.deepEqual(changes[0], { id: 'a', x: 150, y: 150, width: 100, height: 100 });
    await m.event(m.container.querySelector('[data-handle="se"]'), 'pointerdown', 206, 201);
    await m.event(m.viewer(), 'pointermove', 255, 250);
    await m.event(m.viewer(), 'pointerup');
    assert.deepEqual(changes[1], { id: 'a', x: 100, y: 100, width: 150, height: 150 });
    const wheel = new m.dom.window.WheelEvent('wheel', { deltaY: -120, cancelable: true });
    const before = m.container.querySelector('.origin-top-left').style.transform;
    await act(() => m.viewer().dispatchEvent(wheel));
    assert.equal(wheel.defaultPrevented, true);
    assert.notEqual(m.container.querySelector('.origin-top-left').style.transform, before);
  } finally {
    await m.close();
  }
});

test('image navigation, cancellation and permission changes cannot save an unfinished gesture', async () => {
  const calls = [];
  const m = await mount({ options: editing, mode: 'draw', onCreate: (r) => calls.push(r) });
  try {
    await m.event(m.viewer(), 'pointerdown');
    await m.event(m.viewer(), 'pointermove', 206, 201);
    await m.render({ src: 'next-synthetic.svg' });
    await m.loaded();
    await m.event(m.viewer(), 'pointerup');
    await m.event(m.viewer(), 'pointerdown');
    await m.event(m.viewer(), 'pointermove', 206, 201);
    await m.event(m.viewer(), 'pointercancel');
    await m.event(m.viewer(), 'pointerup');
    await m.event(m.viewer(), 'pointerdown');
    await m.event(m.viewer(), 'pointermove', 206, 201);
    await m.render({ options: {} });
    await m.event(m.viewer(), 'pointerup');
    assert.deepEqual(calls, []);
    await act(() => m.container.querySelector('img').dispatchEvent(new m.dom.window.Event('error')));
    assert.match(m.container.textContent, /Couldn't load/);
    await m.render({ src: 'recovered-synthetic.svg' });
    await m.loaded();
    assert.doesNotMatch(m.container.textContent, /Couldn't load/);
  } finally {
    await m.close();
  }
});

test('comparison switches panes and keeps a valid pane visible when a partner disappears', async () => {
  const m = await mount();
  try {
    const container = document.createElement('div');
    // Use a separate root with neutral strings, proving no WFP records are required.
    const comparisonRoot = createRoot(container);
    const panes = [
      { id: 'first', label: 'First view', content: 'First content' },
      { id: 'second', label: 'Second view', content: 'Second content' },
    ];
    let selected = 'both';
    const render = async (items = panes) =>
      act(() =>
        comparisonRoot.render(
          h(ImageComparison, {
            name: 'synthetic',
            panes: items,
            view: selected,
            onView: (v) => {
              selected = v;
            },
          }),
        ),
      );
    await render();
    assert.match(container.textContent, /First content/);
    assert.match(container.textContent, /Second content/);
    await act(() => container.querySelector('input[value="second"]').click());
    await render();
    assert.doesNotMatch(container.textContent, /First content/);
    assert.match(container.textContent, /Second content/);
    await render([panes[0]]);
    assert.match(container.textContent, /First content/);
    await act(() => comparisonRoot.unmount());
  } finally {
    await m.close();
  }
});
