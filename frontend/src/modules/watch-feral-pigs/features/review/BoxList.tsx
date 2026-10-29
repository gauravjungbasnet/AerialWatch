import { Check, RotateCcw, Trash2, X } from 'lucide-react';
import { IconButton, PlainButton } from '@shared/controls';
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
              <PlainButton
                onClick={() => onSelect(b.id)}
                aria-pressed={selected}
                className="flex min-h-11 min-w-0 flex-1 items-center gap-2 rounded text-left"
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
              </PlainButton>

              {editable && b.source === 'ai' && b.status !== 'accepted' && (
                <IconButton
                  title="Accept (A)"
                  label={`Accept box ${i + 1}`}
                  tone="success"
                  onClick={() => onStatus(b.id, 'accepted')}
                  icon={<Check className="size-5" aria-hidden="true" />}
                />
              )}
              {editable && b.source === 'ai' && b.status !== 'rejected' && (
                <IconButton
                  title="Reject (R)"
                  label={`Reject box ${i + 1}`}
                  tone="danger"
                  onClick={() => onStatus(b.id, 'rejected')}
                  icon={<X className="size-5" aria-hidden="true" />}
                />
              )}
              {editable && b.source === 'ai' && b.status !== 'pending' && (
                <IconButton
                  title="Undo (U)"
                  label={`Undo decision on box ${i + 1}`}
                  tone="neutral"
                  onClick={() => onStatus(b.id, 'pending')}
                  icon={<RotateCcw className="size-4" aria-hidden="true" />}
                />
              )}
              {editable && b.source === 'ranger' && (
                <IconButton
                  title="Delete (Del)"
                  label={`Delete box ${i + 1}`}
                  tone="neutral"
                  onClick={() => onDelete(b.id)}
                  icon={<Trash2 className="size-4" aria-hidden="true" />}
                />
              )}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
