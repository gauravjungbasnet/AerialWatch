import { Check, RotateCcw, Trash2, X } from 'lucide-react';
import type { Detection } from '../../api/types';
import { cx, percent } from '@shared/lib/format';
import { boxStyle } from '../../domain/boxAppearance';

interface BoxListProps {
  boxes: Detection[];
  selectedId: string | null;
  editable: boolean;
  onSelect(id: string): void;
  onStatus(id: string, status: Detection['status']): void;
  onDelete(id: string): void;
}

const iconBtn =
  'inline-flex size-11 items-center justify-center rounded-lg border text-sm focus:outline-none focus-visible:ring-focus disabled:cursor-not-allowed disabled:opacity-40';

/**
 * Every box on the photo as a list (same numbers as on the photo): the keyboard- and screen-reader-friendly way to
 * accept, reject, undo or delete without touching the image.
 */
export function BoxList({ boxes, selectedId, editable, onSelect, onStatus, onDelete }: BoxListProps) {
  if (boxes.length === 0)
    return (
      <p className="text-sm text-gray-600 dark:text-gray-400">
        No boxes on this photo. If you can see a pig, press <strong>N</strong> (or choose <em>Draw box</em>) and drag
        around it.
      </p>
    );

  return (
    <ol className="space-y-2">
      {boxes.map((b, i) => {
        const st = boxStyle(b);
        const selected = b.id === selectedId;
        const what =
          b.source === 'ranger'
            ? 'Drawn by ranger'
            : `AI${b.confidence != null ? ` · ${percent(b.confidence)} sure` : ''}`;
        return (
          <li
            key={b.id}
            className={cx(
              'rounded-lg border p-3 transition-colors hover:border-brand-300',
              selected ? 'border-brand-500 bg-brand-50 dark:bg-gray-700' : 'border-gray-200 dark:border-gray-700',
            )}
          >
            <div className="flex items-center gap-2">
              <button
                type="button"
                onClick={() => onSelect(b.id)}
                aria-pressed={selected}
                className="flex min-h-11 min-w-0 flex-1 items-center gap-2 rounded text-left focus:outline-none focus-visible:ring-focus"
              >
                <span
                  aria-hidden="true"
                  className="grid size-7 shrink-0 place-items-center rounded text-xs font-bold text-gray-900"
                  style={{ background: st.color }}
                >
                  {i + 1}
                </span>
                <span className="min-w-0">
                  <span className="block truncate text-sm font-medium text-gray-800 dark:text-gray-200">{what}</span>
                  <span className="block text-xs text-gray-500 dark:text-gray-400">
                    {st.label}
                    {b.reviewedBy && b.status !== 'pending' ? ` · ${b.reviewedBy}` : ''}
                  </span>
                </span>
              </button>

              {editable && b.source === 'ai' && b.status !== 'accepted' && (
                <button
                  type="button"
                  title="Accept (A)"
                  aria-label={`Accept box ${i + 1}`}
                  onClick={() => onStatus(b.id, 'accepted')}
                  className={cx(
                    iconBtn,
                    'border-green-300 text-green-700 hover:bg-green-50 dark:border-green-700 dark:text-green-300 dark:hover:bg-green-900/30',
                  )}
                >
                  <Check className="size-5" aria-hidden="true" />
                </button>
              )}
              {editable && b.source === 'ai' && b.status !== 'rejected' && (
                <button
                  type="button"
                  title="Reject (R)"
                  aria-label={`Reject box ${i + 1}`}
                  onClick={() => onStatus(b.id, 'rejected')}
                  className={cx(
                    iconBtn,
                    'border-red-300 text-red-700 hover:bg-red-50 dark:border-red-700 dark:text-red-300 dark:hover:bg-red-900/30',
                  )}
                >
                  <X className="size-5" aria-hidden="true" />
                </button>
              )}
              {editable && b.source === 'ai' && b.status !== 'pending' && (
                <button
                  type="button"
                  title="Undo (U)"
                  aria-label={`Undo decision on box ${i + 1}`}
                  onClick={() => onStatus(b.id, 'pending')}
                  className={cx(
                    iconBtn,
                    'border-gray-300 text-gray-600 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700',
                  )}
                >
                  <RotateCcw className="size-4" aria-hidden="true" />
                </button>
              )}
              {editable && b.source === 'ranger' && (
                <button
                  type="button"
                  title="Delete (Del)"
                  aria-label={`Delete box ${i + 1}`}
                  onClick={() => onDelete(b.id)}
                  className={cx(
                    iconBtn,
                    'border-gray-300 text-gray-600 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700',
                  )}
                >
                  <Trash2 className="size-4" aria-hidden="true" />
                </button>
              )}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
