import type { ReactNode } from 'react';
import { Check, EyeOff, RotateCcw, Trash2 } from 'lucide-react';
import type { CameraType, TwinCheck, TwinImage, TwinMark, TwinSighting } from '../../api/types';
import { Card } from '@shared/controls';
import { cx } from '@shared/lib/format';
import { cameraName } from '../../domain/cameras';
import { sightingId } from '../../adapters/annotations';

interface Props {
  twin: TwinImage;
  countedCamera: CameraType;
  check?: TwinCheck;
  loading: boolean;
  editable: boolean;
  selectedId: string | null;
  onSelect(id: string): void;
  onPlace(mark: TwinMark): void;
  onNotVisible(mark: TwinMark): void;
  onReset(mark: TwinMark): void;
  onConfirmSighting(s: TwinSighting): void;
  onSightingNotVisible(s: TwinSighting, notVisible: boolean): void;
  onRemoveSighting(s: TwinSighting): void;
}

const btn =
  'inline-flex size-11 shrink-0 items-center justify-center rounded-lg border text-sm focus:outline-none focus-visible:ring-focus disabled:cursor-not-allowed disabled:opacity-40';
const ok = cx(btn, 'border-green-300 text-green-700 hover:bg-green-50 dark:text-green-300');
const plain = cx(btn, 'border-gray-300 text-gray-700 hover:bg-gray-50 dark:text-gray-300');

const markText = { suggested: 'Suggested position', confirmed: 'Placed', notVisible: 'Not visible' } as const;

/**
 * Both directions of the twin check (D-040, D-041). Pigs on the counted photo: ✓ the suggested spot on the other photo
 * is right (or drag it there), or not visible. Pigs drawn on the other photo first: ✓ the suggested spot on the counted
 * photo is right (this counts the pig), or not visible there, or remove. Only counted-photo boxes are ever counted.
 */
export function TwinCheckCard({
  twin,
  countedCamera,
  check,
  loading,
  editable,
  selectedId,
  onSelect,
  onPlace,
  onNotVisible,
  onReset,
  onConfirmSighting,
  onSightingNotVisible,
  onRemoveSighting,
}: Props) {
  const twinName = cameraName(twin.camera).toLowerCase();
  const countedName = cameraName(countedCamera).toLowerCase();
  const marks = check?.marks ?? [];
  const sightings = check?.sightings ?? [];
  const done = marks.filter((m) => m.state !== 'suggested').length;

  return (
    <Card
      title={`${cameraName(twin.camera)} check (${done}/${marks.length})`}
      description={`Place suggestions or draw on either photo (N or Ctrl/Cmd + drag). Only ${countedName} boxes are counted.`}
    >
      {loading && marks.length === 0 && sightings.length === 0 ? (
        <p className="text-sm text-gray-500 dark:text-gray-400">Loading…</p>
      ) : (
        <ol className="space-y-2">
          {marks.map((m) => (
            <Row
              key={m.detectionId}
              id={m.detectionId}
              selectedId={selectedId}
              onSelect={onSelect}
              title={`Pig ${m.number}`}
              detail={`${markText[m.state]} on ${twinName}`}
            >
              {editable && (
                <>
                  <button
                    type="button"
                    className={ok}
                    title="Position is correct"
                    aria-label={`Pig ${m.number}: position on ${twinName} is correct`}
                    disabled={m.x == null || m.state === 'confirmed'}
                    onClick={() => onPlace(m)}
                  >
                    <Check className="size-4" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className={plain}
                    title={`Not visible on ${twinName}`}
                    aria-label={`Pig ${m.number}: not visible on ${twinName}`}
                    disabled={m.state === 'notVisible'}
                    onClick={() => onNotVisible(m)}
                  >
                    <EyeOff className="size-4" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className={plain}
                    title="Back to the suggestion"
                    aria-label={`Pig ${m.number}: back to the suggestion`}
                    disabled={m.state === 'suggested'}
                    onClick={() => onReset(m)}
                  >
                    <RotateCcw className="size-4" aria-hidden="true" />
                  </button>
                </>
              )}
            </Row>
          ))}
          {sightings.map((s) => (
            <Row
              key={s.markId}
              id={sightingId(s.markId)}
              selectedId={selectedId}
              onSelect={onSelect}
              title={`${s.label} · seen on ${twinName}`}
              detail={
                s.state === 'notVisibleOnCounted'
                  ? `Not visible on ${countedName} (not counted)`
                  : `Suggested on ${countedName}: ✓ to count it`
              }
            >
              {editable && (
                <>
                  <button
                    type="button"
                    className={ok}
                    title={`Correct: count it on ${countedName}`}
                    aria-label={`${s.label}: position on ${countedName} is correct, count it`}
                    disabled={s.x == null || s.state === 'notVisibleOnCounted'}
                    onClick={() => onConfirmSighting(s)}
                  >
                    <Check className="size-4" aria-hidden="true" />
                  </button>
                  <button
                    type="button"
                    className={plain}
                    title={
                      s.state === 'notVisibleOnCounted' ? 'Back to the suggestion' : `Not visible on ${countedName}`
                    }
                    aria-label={`${s.label}: ${s.state === 'notVisibleOnCounted' ? 'back to the suggestion' : `not visible on ${countedName}`}`}
                    onClick={() => onSightingNotVisible(s, s.state !== 'notVisibleOnCounted')}
                  >
                    {s.state === 'notVisibleOnCounted' ? (
                      <RotateCcw className="size-4" aria-hidden="true" />
                    ) : (
                      <EyeOff className="size-4" aria-hidden="true" />
                    )}
                  </button>
                  <button
                    type="button"
                    className={plain}
                    title="Remove"
                    aria-label={`Remove ${s.label}`}
                    onClick={() => onRemoveSighting(s)}
                  >
                    <Trash2 className="size-4" aria-hidden="true" />
                  </button>
                </>
              )}
            </Row>
          ))}
          {marks.length === 0 && sightings.length === 0 && (
            <li className="text-sm text-gray-600 dark:text-gray-400">
              Confirm a pig on the {countedName} photo, or draw one on the {twinName} photo, to check it on the other.
            </li>
          )}
        </ol>
      )}
      {check && check.learnedFrom > 0 && (
        <p className="mt-3 rounded-lg bg-brand-50 p-3 text-xs text-brand-800 dark:bg-brand-900/30 dark:text-brand-200">
          Suggestions learn from the {check.learnedFrom} pig{check.learnedFrom === 1 ? '' : 's'} already placed on both
          photos.
        </p>
      )}
    </Card>
  );
}

function Row({
  id,
  selectedId,
  onSelect,
  title,
  detail,
  children,
}: {
  id: string;
  selectedId: string | null;
  onSelect(id: string): void;
  title: string;
  detail: string;
  children?: ReactNode;
}) {
  return (
    <li
      className={cx(
        'flex items-center gap-2 rounded-lg border p-2',
        id === selectedId ? 'border-brand-500 bg-brand-50 dark:bg-gray-700' : 'border-gray-200 dark:border-gray-700',
      )}
    >
      <button
        type="button"
        className="min-h-11 flex-1 rounded-md px-1 text-left text-sm focus:outline-none focus-visible:ring-focus"
        onClick={() => onSelect(id)}
      >
        <span className="font-semibold">{title}</span>
        <span className="block text-xs text-gray-600 dark:text-gray-400">{detail}</span>
      </button>
      {children}
    </li>
  );
}
