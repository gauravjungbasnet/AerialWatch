import {
  forwardRef,
  useCallback,
  useEffect,
  useImperativeHandle,
  useLayoutEffect,
  useRef,
  useState,
  type PointerEvent as ReactPointerEvent,
} from 'react';
import type { Rect } from '../annotations/types';
import { resolveViewerOptions, type ViewerOptions } from './options';
import { cx } from '../../lib/format';
import { drawGestureRequested } from './gestures';
import { clamp, rectFromPoints, roundRect, isBigEnough } from '../annotations/geometry';

export type ViewerMode = 'select' | 'draw';

export interface ImageViewerHandle {
  fit(): void;
  zoomBy(factor: number): void;
}

export interface ViewerAnnotation extends Rect {
  id: string;
  label: string;
  appearance: { color: string; fill?: string; dash?: number[]; opacity?: number };
  can?: { move?: boolean; resize?: boolean };
}

export interface ImageViewerProps {
  src: string;
  alt: string;
  annotations: ViewerAnnotation[];
  selectedId?: string | null;
  onSelect?(id: string | null): void;
  mode?: ViewerMode;
  options?: ViewerOptions;
  onCreate?(rect: Rect): void;
  onChangeBox?(id: string, rect: Rect): void;
}

type Handle = 'nw' | 'ne' | 'sw' | 'se';
type Point = { x: number; y: number };
type Drag =
  | { kind: 'pan'; start: Point; tx: number; ty: number; moved: boolean }
  | { kind: 'draw'; start: Point; current: Point }
  | { kind: 'move'; id: string; start: Point; orig: Rect; current: Rect }
  | { kind: 'resize'; id: string; handle: Handle; orig: Rect; current: Rect };

const MIN_SCALE = 0.05;
const MAX_SCALE = 16;

/** Shared image viewport and rectangle editor. Coordinates remain original-image pixels; callers own persistence. */
const ImageViewerContent = forwardRef<ImageViewerHandle, ImageViewerProps>(function ImageViewer(
  { src, alt, annotations: boxes, selectedId = null, onSelect, mode = 'select', options, onCreate, onChangeBox },
  ref,
) {
  const config = resolveViewerOptions(options, {
    create: Boolean(onCreate),
    change: Boolean(onChangeBox),
    select: Boolean(onSelect),
  });
  const editable = config.draw || config.move || config.resize;
  const containerRef = useRef<HTMLDivElement>(null);
  // Size of the loaded photo, tagged with its URL so a new photo never uses the old size (even if it loads instantly from cache).
  const [loaded, setLoaded] = useState<{ src: string; w: number; h: number } | null>(null);
  const natural = loaded?.src === src ? loaded : null;
  const [view, setView] = useState({ scale: 1, tx: 0, ty: 0 });
  const [drag, setDrag] = useState<Drag | null>(null);
  const [modifierDraw, setModifierDraw] = useState(false);
  const fitted = useRef(true);
  const [failed, setFailed] = useState(false);

  useEffect(() => {
    if (!config.draw) return;
    const onKey = (e: KeyboardEvent) => setModifierDraw(e.ctrlKey || e.metaKey);
    const reset = () => setModifierDraw(false);
    window.addEventListener('keydown', onKey);
    window.addEventListener('keyup', onKey);
    window.addEventListener('blur', reset);
    return () => {
      window.removeEventListener('keydown', onKey);
      window.removeEventListener('keyup', onKey);
      window.removeEventListener('blur', reset);
    };
  }, [config.draw]);

  // Revoking a capability or switching tools cancels an unfinished pointer operation.
  useEffect(() => {
    setDrag(null);
    setModifierDraw(false);
  }, [config.draw, config.move, config.resize, config.pan, mode]);

  const fit = useCallback(() => {
    const el = containerRef.current;
    if (!el || !natural) return;
    const { clientWidth: cw, clientHeight: ch } = el;
    const scale = Math.min(cw / natural.w, ch / natural.h) * 0.98;
    setView({ scale, tx: (cw - natural.w * scale) / 2, ty: (ch - natural.h * scale) / 2 });
    fitted.current = true;
  }, [natural]);

  const zoomAt = useCallback(
    (factor: number, cx0?: number, cy0?: number) => {
      const el = containerRef.current;
      if (!el || !config.zoom) return;
      const px = cx0 ?? el.clientWidth / 2;
      const py = cy0 ?? el.clientHeight / 2;
      setView((v) => {
        const scale = clamp(v.scale * factor, MIN_SCALE, MAX_SCALE);
        const k = scale / v.scale;
        return { scale, tx: px - (px - v.tx) * k, ty: py - (py - v.ty) * k };
      });
      fitted.current = false;
    },
    [config.zoom],
  );

  useImperativeHandle(ref, () => ({ fit, zoomBy: (f: number) => zoomAt(f) }), [fit, zoomAt]);

  useLayoutEffect(() => {
    fit();
  }, [fit]);

  // Keep the photo fitted when the window or layout changes, unless the user has zoomed in.
  useEffect(() => {
    const el = containerRef.current;
    if (!el) return;
    const ro = new ResizeObserver(() => fitted.current && fit());
    ro.observe(el);
    return () => ro.disconnect();
  }, [fit]);

  // Wheel zoom needs a non-passive listener so the page doesn't scroll at the same time.
  useEffect(() => {
    const el = containerRef.current;
    if (!el || !config.zoom) return;
    const onWheel = (e: WheelEvent) => {
      e.preventDefault();
      const r = el.getBoundingClientRect();
      zoomAt(Math.exp(-e.deltaY * 0.0015), e.clientX - r.left, e.clientY - r.top);
    };
    el.addEventListener('wheel', onWheel, { passive: false });
    return () => el.removeEventListener('wheel', onWheel);
  }, [zoomAt, config.zoom]);

  const toImage = (e: { clientX: number; clientY: number }): Point => {
    const r = containerRef.current!.getBoundingClientRect();
    return { x: (e.clientX - r.left - view.tx) / view.scale, y: (e.clientY - r.top - view.ty) / view.scale };
  };

  const fromCorners = (a: Point, b: Point): Rect =>
    rectFromPoints(a, b, natural?.w ?? Infinity, natural?.h ?? Infinity);

  const onPointerDown = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (e.button !== 0 || !natural || !(config.pan || config.selection || editable)) return;
    const target = e.target as Element;
    const boxId = target.closest('[data-box-id]')?.getAttribute('data-box-id') ?? null;
    const handle = target.getAttribute('data-handle') as Handle | null;
    const box = boxId ? boxes.find((b) => b.id === boxId) : undefined;
    const p = toImage(e);
    e.currentTarget.setPointerCapture(e.pointerId);

    if (drawGestureRequested('select', config.draw, e.ctrlKey, e.metaKey)) {
      e.preventDefault();
      setDrag({ kind: 'draw', start: p, current: p });
    } else if (box && handle && config.resize && box.can?.resize !== false) {
      setDrag({ kind: 'resize', id: box.id, handle, orig: box, current: box });
    } else if (box && mode === 'select' && (config.selection || config.move)) {
      if (config.selection) onSelect?.(box.id);
      if (config.move && box.can?.move !== false)
        setDrag({ kind: 'move', id: box.id, start: p, orig: box, current: box });
    } else if (drawGestureRequested(mode, config.draw, false, false)) {
      setDrag({ kind: 'draw', start: p, current: p });
    } else if (config.pan) {
      setDrag({ kind: 'pan', start: { x: e.clientX, y: e.clientY }, tx: view.tx, ty: view.ty, moved: false });
    }
  };

  const onPointerMove = (e: ReactPointerEvent<HTMLDivElement>) => {
    if (!drag || !natural) return;
    if (drag.kind === 'pan') {
      const dx = e.clientX - drag.start.x;
      const dy = e.clientY - drag.start.y;
      setView((v) => ({ ...v, tx: drag.tx + dx, ty: drag.ty + dy }));
      fitted.current = false;
      if (!drag.moved && Math.hypot(dx, dy) > 3) setDrag({ ...drag, moved: true });
      return;
    }
    const p = toImage(e);
    if (drag.kind === 'draw') setDrag({ ...drag, current: p });
    else if (drag.kind === 'move') {
      const o = drag.orig;
      const x = clamp(o.x + p.x - drag.start.x, 0, natural.w - o.width);
      const y = clamp(o.y + p.y - drag.start.y, 0, natural.h - o.height);
      setDrag({ ...drag, current: { x, y, width: o.width, height: o.height } });
    } else {
      const o = drag.orig;
      const fixed: Point = {
        x: drag.handle.includes('w') ? o.x + o.width : o.x,
        y: drag.handle.includes('n') ? o.y + o.height : o.y,
      };
      setDrag({ ...drag, current: fromCorners(fixed, p) });
    }
  };

  const onPointerUp = () => {
    const d = drag;
    setDrag(null);
    if (!d) return;
    if (d.kind === 'pan') {
      if (!d.moved && config.selection) onSelect?.(null); // a click on empty space clears the selection
    } else if (d.kind === 'draw') {
      const rect = fromCorners(d.start, d.current);
      if (config.draw && isBigEnough(rect, config.minBoxSize)) onCreate?.(roundRect(rect));
    } else {
      const r = roundRect(d.current);
      const o = d.orig;
      const changed = r.x !== o.x || r.y !== o.y || r.width !== o.width || r.height !== o.height;
      if ((d.kind === 'move' ? config.move : config.resize) && changed && isBigEnough(r, config.minBoxSize))
        onChangeBox?.(d.id, r);
    }
  };

  // The box being dragged is drawn at its live position.
  const shown = boxes.map((b) =>
    drag && (drag.kind === 'move' || drag.kind === 'resize') && drag.id === b.id ? { ...b, ...drag.current } : b,
  );
  const draft = drag?.kind === 'draw' ? fromCorners(drag.start, drag.current) : null;
  const s = view.scale;

  return (
    <div
      ref={containerRef}
      role="img"
      aria-label={`${alt}: ${boxes.length} annotation${boxes.length === 1 ? '' : 's'}.`}
      className={cx(
        'relative h-full w-full select-none overflow-hidden bg-gray-900',
        (config.pan || editable) && 'touch-none',
        (mode === 'draw' || modifierDraw) && config.draw
          ? 'cursor-crosshair'
          : drag?.kind === 'pan'
            ? 'cursor-grabbing'
            : config.pan
              ? 'cursor-grab'
              : 'cursor-default',
      )}
      onPointerDown={onPointerDown}
      onPointerMove={onPointerMove}
      onPointerUp={onPointerUp}
      onPointerCancel={() => setDrag(null)}
      onLostPointerCapture={() => setDrag(null)}
      onContextMenu={(e) => {
        if (editable && (e.ctrlKey || e.metaKey || drag?.kind === 'draw')) e.preventDefault();
      }}
      onDoubleClick={config.zoom ? fit : undefined}
    >
      <div
        className="absolute left-0 top-0 origin-top-left"
        style={{
          transform: `translate(${view.tx}px, ${view.ty}px) scale(${s})`,
          width: natural?.w,
          height: natural?.h,
          visibility: natural ? 'visible' : 'hidden',
        }}
      >
        <img
          key={src}
          src={src}
          alt=""
          draggable={false}
          className="block max-w-none"
          style={natural ? { width: natural.w, height: natural.h } : undefined}
          onError={() => {
            setFailed(true);
            setLoaded(null);
          }}
          onLoad={(e) => setLoaded({ src, w: e.currentTarget.naturalWidth, h: e.currentTarget.naturalHeight })}
        />
        {natural && (
          <svg
            className="absolute inset-0 overflow-visible"
            width={natural.w}
            height={natural.h}
            viewBox={`0 0 ${natural.w} ${natural.h}`}
          >
            {shown.map((b) => {
              const st = b.appearance;
              const selected = b.id === selectedId;
              const hs = 10 / s; // handle size: 10 screen px
              return (
                <g
                  key={b.id}
                  data-box-id={b.id}
                  className={
                    modifierDraw && editable
                      ? 'cursor-crosshair'
                      : config.move && b.can?.move !== false && mode === 'select'
                        ? 'cursor-move'
                        : undefined
                  }
                  opacity={st.opacity}
                >
                  {/* Wide transparent stroke = easier to grab a thin box */}
                  <rect
                    x={b.x}
                    y={b.y}
                    width={b.width}
                    height={b.height}
                    fill="transparent"
                    stroke="transparent"
                    strokeWidth={12 / s}
                  />
                  <rect
                    x={b.x}
                    y={b.y}
                    width={b.width}
                    height={b.height}
                    fill={selected ? (st.fill ?? 'transparent') : 'transparent'}
                    stroke={st.color}
                    strokeWidth={(selected ? 3 : 2) / s}
                    strokeDasharray={st.dash ? st.dash.map((v) => v / s).join(' ') : undefined}
                  />
                  {(config.labels === 'all' || (selected && config.labels === 'selected')) && (
                    <text
                      x={b.x}
                      y={b.y * s < 18 ? b.y + 14 / s : b.y - 4 / s}
                      fontSize={12 / s}
                      fontWeight={600}
                      fill={st.color}
                      stroke="black"
                      strokeWidth={3 / s}
                      paintOrder="stroke"
                      className="pointer-events-none"
                    >
                      {b.label}
                    </text>
                  )}
                  {selected &&
                    config.resize &&
                    b.can?.resize !== false &&
                    (['nw', 'ne', 'sw', 'se'] as Handle[]).map((h) => (
                      <rect
                        key={h}
                        data-handle={h}
                        x={(h.includes('w') ? b.x : b.x + b.width) - hs / 2}
                        y={(h.includes('n') ? b.y : b.y + b.height) - hs / 2}
                        width={hs}
                        height={hs}
                        fill="white"
                        stroke={st.color}
                        strokeWidth={1.5 / s}
                        className={
                          modifierDraw
                            ? 'cursor-crosshair'
                            : h === 'nw' || h === 'se'
                              ? 'cursor-nwse-resize'
                              : 'cursor-nesw-resize'
                        }
                      />
                    ))}
                </g>
              );
            })}
            {draft && (
              <rect
                x={draft.x}
                y={draft.y}
                width={draft.width}
                height={draft.height}
                fill="transparent"
                stroke="var(--color-sky-400)"
                strokeWidth={2 / s}
                strokeDasharray={`${6 / s} ${4 / s}`}
              />
            )}
          </svg>
        )}
      </div>

      {!natural && (
        <p className="absolute inset-0 grid place-items-center text-sm text-gray-300">
          {failed ? "Couldn't load this image." : 'Loading image…'}
        </p>
      )}
      {config.showHint && (
        <p className="pointer-events-none absolute bottom-2 left-2 max-w-[calc(100%-1rem)] rounded bg-black/60 px-2 py-1 text-xs leading-relaxed text-white">
          {Math.round(s * 100)}% ·{' '}
          {mode === 'draw' && editable
            ? 'Drag to draw a box'
            : `${config.pan ? 'Drag to pan' : ''}${config.zoom ? ' · scroll to zoom' : ''}${config.draw ? ' · Ctrl/Cmd + drag to draw' : ''}`}
        </p>
      )}
    </div>
  );
});

/** Reset gestures and failed loads on image navigation; persistence stays in the caller. */
export const ImageViewer = forwardRef<ImageViewerHandle, ImageViewerProps>(function ImageViewer(props, ref) {
  return <ImageViewerContent key={props.src} ref={ref} {...props} />;
});
