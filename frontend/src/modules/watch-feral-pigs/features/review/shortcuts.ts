import { useKeyboardShortcuts } from '@shared/review/useKeyboardShortcuts';

export type ShortcutAction =
  | 'nextPhoto'
  | 'prevPhoto'
  | 'nextBox'
  | 'prevBox'
  | 'accept'
  | 'acceptAll'
  | 'reject'
  | 'undo'
  | 'undoDrawing'
  | 'redoDrawing'
  | 'delete'
  | 'draw'
  | 'escape'
  | 'checkAndNext'
  | 'flag'
  | 'zoomIn'
  | 'zoomOut'
  | 'fit'
  | 'help';

/** Shown in the help card; keys in the order a ranger uses them. */
export const SHORTCUTS: { keys: string[]; action: string }[] = [
  { keys: ['A'], action: 'Accept the selected suggestion' },
  { keys: ['R'], action: 'Reject the selected suggestion' },
  { keys: ['Shift', 'A'], action: 'Accept every suggestion on this photo' },
  { keys: ['U'], action: 'Undo (back to "to check")' },
  { keys: [']', '['], action: 'Next / previous box' },
  { keys: ['N'], action: 'Draw a box (a pig the AI missed)' },
  { keys: ['Ctrl/Cmd', 'drag'], action: 'Temporarily draw a box' },
  { keys: ['Ctrl/Cmd', 'Z'], action: 'Undo your last drawing on this photo' },
  { keys: ['Ctrl/Cmd', 'Y'], action: 'Redo drawing (also Cmd/Ctrl + Shift + Z)' },
  { keys: ['Del'], action: 'Delete your box, or reject a suggestion' },
  { keys: ['Space'], action: 'Photo checked, go to the next one' },
  { keys: ['→', '←'], action: 'Next / previous photo' },
  { keys: ['F'], action: 'Flag for a second look' },
  { keys: ['+', '−', '0'], action: 'Zoom in / out / fit' },
  { keys: ['Esc'], action: 'Stop drawing, clear selection' },
  { keys: ['?'], action: 'Show or hide these shortcuts' },
];

function actionFor(e: KeyboardEvent): ShortcutAction | null {
  if (e.altKey) return null;
  if (e.ctrlKey || e.metaKey) {
    const key = e.key.toLowerCase();
    if (key === 'z') return e.shiftKey ? 'redoDrawing' : 'undoDrawing';
    if (key === 'y' && !e.shiftKey) return 'redoDrawing';
    return null;
  }
  switch (e.key) {
    case 'ArrowRight':
      return 'nextPhoto';
    case 'ArrowLeft':
      return 'prevPhoto';
    case ']':
      return 'nextBox';
    case '[':
      return 'prevBox';
    case 'a':
      return 'accept';
    case 'A':
      return e.shiftKey ? 'acceptAll' : 'accept';
    case 'r':
    case 'R':
      return 'reject';
    case 'u':
    case 'U':
      return 'undo';
    case 'Delete':
    case 'Backspace':
      return 'delete';
    case 'n':
    case 'N':
      return 'draw';
    case 'Escape':
      return 'escape';
    case ' ':
      return 'checkAndNext';
    case 'f':
    case 'F':
      return 'flag';
    case '+':
    case '=':
      return 'zoomIn';
    case '-':
    case '_':
      return 'zoomOut';
    case '0':
      return 'fit';
    case '?':
      return 'help';
    default:
      return null;
  }
}

/** Global review shortcuts (WFP key map on the shared keyboard hook). Off before the review starts. */
export function useReviewShortcuts(handler: (action: ShortcutAction) => void, enabled: boolean) {
  useKeyboardShortcuts(actionFor, handler, { enabled, noRepeat: ['undoDrawing', 'redoDrawing'] });
}
