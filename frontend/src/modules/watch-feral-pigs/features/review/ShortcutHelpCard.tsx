import { Keyboard } from 'lucide-react';
import { Button, Card, Kbd } from '@shared/controls';
import { SHORTCUTS } from './shortcuts';

/** The common keys, or the full list when opened (also toggled with ?). */
export function ShortcutHelpCard({ open, onToggle }: { open: boolean; onToggle(): void }) {
  return (
    <Card
      title={
        <span className="inline-flex items-center gap-2">
          <Keyboard className="size-4" aria-hidden="true" /> Keyboard shortcuts
        </span>
      }
      actions={
        <Button variant="link" size="sm" aria-expanded={open} onClick={onToggle}>
          {open ? 'Hide' : 'Show'}
        </Button>
      }
    >
      {open ? (
        <dl className="space-y-1.5 text-sm">
          {SHORTCUTS.map((k) => (
            <div key={k.action} className="flex items-start justify-between gap-3">
              <dt className="flex shrink-0 gap-1">
                {k.keys.map((key) => (
                  <Kbd key={key}>{key}</Kbd>
                ))}
              </dt>
              <dd className="text-right text-gray-600 dark:text-gray-400">{k.action}</dd>
            </div>
          ))}
        </dl>
      ) : (
        <p className="text-sm text-gray-600 dark:text-gray-400">
          <Kbd>A</Kbd> accept · <Kbd>R</Kbd> reject · <Kbd>N</Kbd> draw · <Kbd>Space</Kbd> next. Press <Kbd>?</Kbd> for
          all.
        </p>
      )}
    </Card>
  );
}
