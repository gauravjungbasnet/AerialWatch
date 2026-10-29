import {
  ChevronLeft,
  ChevronRight,
  Maximize,
  MousePointer2,
  Redo2,
  SquareDashed,
  Undo2,
  ZoomIn,
  ZoomOut,
} from 'lucide-react';
import type { ReactNode } from 'react';
import { Button, ToggleButton } from '@shared/controls';
import { ntTime } from '@shared/lib/dates';
import type { ViewerMode } from './ImageViewer';

interface ReviewToolbarProps {
  /** "Photo 3 of 40", "Loading…" or "No photos". */
  position: string;
  canPrev: boolean;
  canNext: boolean;
  onPrev(): void;
  onNext(): void;
  fileName?: string;
  takenAt?: string;
  loading: boolean;
  mode: ViewerMode;
  onMode(mode: ViewerMode): void;
  editable: boolean;
  canUndo: boolean;
  canRedo: boolean;
  onUndo(): void;
  onRedo(): void;
  onZoom(factor: number): void;
  onFit(): void;
}

/** Photo navigation on the left; Select / Draw, drawing undo/redo and zoom on the right. */
export function ReviewToolbar(p: ReviewToolbarProps) {
  return (
    <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-200 bg-gray-50/70 px-3 py-3 dark:border-gray-700 dark:bg-gray-900/30">
      <div className="flex min-w-0 flex-wrap items-center gap-3">
        <div className="flex shrink-0 items-center rounded-lg border border-gray-200 bg-white dark:border-gray-600 dark:bg-gray-800">
          <Button
            variant="ghost"
            className="min-h-11 rounded-r-none border-r border-gray-200! px-2! dark:border-gray-600!"
            onClick={p.onPrev}
            disabled={!p.canPrev}
            aria-label="Previous photo (←)"
            icon={<ChevronLeft size={16} aria-hidden="true" />}
          >
            <span className="hidden sm:inline">Prev</span>
          </Button>
          <p className="px-3 text-sm font-semibold text-gray-900 dark:text-gray-100" aria-live="polite">
            {p.position}
          </p>
          <Button
            variant="ghost"
            className="min-h-11 rounded-l-none border-l border-gray-200! px-2! dark:border-gray-600!"
            onClick={p.onNext}
            disabled={!p.canNext}
            aria-label="Next photo (→)"
          >
            <span className="hidden sm:inline">Next</span>
            <ChevronRight size={16} aria-hidden="true" />
          </Button>
        </div>
        <p className="max-w-56 truncate text-xs text-gray-600 dark:text-gray-400" title={p.fileName}>
          {p.fileName ?? (p.loading ? 'Loading photo…' : 'Choose a photo')}
          {p.takenAt && <span className="ml-2">{ntTime(p.takenAt)}</span>}
        </p>
      </div>
      <div className="flex flex-wrap items-center gap-1" role="group" aria-label="Photo editing tools">
        <ToggleButton
          active={p.mode === 'select'}
          onClick={() => p.onMode('select')}
          icon={<MousePointer2 size={16} aria-hidden="true" />}
          label="Select"
        />
        <ToggleButton
          active={p.mode === 'draw'}
          disabled={!p.editable}
          onClick={() => p.onMode('draw')}
          icon={<SquareDashed size={16} aria-hidden="true" />}
          label="Draw box (N)"
        />
        <span className="mx-1 h-6 w-px bg-gray-200 dark:bg-gray-600" aria-hidden="true" />
        <ToolIcon
          label="Undo drawing (Ctrl or Cmd + Z)"
          title="Undo drawing (Ctrl/Cmd + Z)"
          disabled={!p.canUndo}
          onClick={p.onUndo}
          icon={<Undo2 size={16} aria-hidden="true" />}
        />
        <ToolIcon
          label="Redo drawing (Ctrl or Cmd + Y)"
          title="Redo drawing (Ctrl/Cmd + Y)"
          disabled={!p.canRedo}
          onClick={p.onRedo}
          icon={<Redo2 size={16} aria-hidden="true" />}
        />
        <ToolIcon
          label="Zoom out (−)"
          title="Zoom out"
          onClick={() => p.onZoom(0.8)}
          icon={<ZoomOut size={16} aria-hidden="true" />}
        />
        <ToolIcon
          label="Zoom in (+)"
          title="Zoom in"
          onClick={() => p.onZoom(1.25)}
          icon={<ZoomIn size={16} aria-hidden="true" />}
        />
        <ToolIcon
          label="Fit photo (0)"
          title="Fit photos"
          onClick={p.onFit}
          icon={<Maximize size={16} aria-hidden="true" />}
        />
      </div>
    </div>
  );
}

function ToolIcon({
  label,
  title,
  icon,
  onClick,
  disabled,
}: {
  label: string;
  title: string;
  icon: ReactNode;
  onClick(): void;
  disabled?: boolean;
}) {
  return (
    <Button
      variant="ghost"
      className="size-11 p-0!"
      disabled={disabled}
      aria-label={label}
      title={title}
      onClick={onClick}
      icon={icon}
    />
  );
}
