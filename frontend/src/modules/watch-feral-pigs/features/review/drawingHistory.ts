import type { BoxRect, Detection } from '../../api/types';

interface DrawingPort {
  add(imageId: string, rect: BoxRect): Promise<Detection>;
  remove(id: string): Promise<void>;
}
interface History {
  undo: Detection[];
  redo: BoxRect[];
}
const rectOf = ({ x, y, width, height }: BoxRect): BoxRect => ({ x, y, width, height });

/** Session history for newly drawn ranger boxes only. Stacks change only after the server succeeds. */
export class DrawingHistory {
  private histories = new Map<string, History>();
  busy = false;

  private history(imageId: string): History {
    let history = this.histories.get(imageId);
    if (!history) {
      history = { undo: [], redo: [] };
      this.histories.set(imageId, history);
    }
    return history;
  }

  canUndo(imageId: string, boxes: Detection[]) {
    return this.history(imageId).undo.some((entry) =>
      boxes.some((box) => box.id === entry.id && box.source === 'ranger'),
    );
  }
  canRedo(imageId: string) {
    return this.history(imageId).redo.length > 0;
  }

  async execute(
    imageId: string,
    command: 'draw' | 'undo' | 'redo',
    port: DrawingPort,
    boxes: Detection[],
    rect?: BoxRect,
  ): Promise<Detection | undefined> {
    if (this.busy) return;
    this.busy = true;
    const history = this.history(imageId);
    try {
      if (command === 'draw' && rect) {
        const created = await port.add(imageId, rectOf(rect));
        history.undo.push(created);
        history.undo = history.undo.slice(-100);
        history.redo = [];
        return created;
      }
      if (command === 'undo') {
        // A box explicitly deleted elsewhere must not prevent undoing an earlier drawing.
        let index = history.undo.length - 1;
        while (index >= 0 && !boxes.some((box) => box.id === history.undo[index].id && box.source === 'ranger'))
          index--;
        if (index < 0) return;
        const current = boxes.find((box) => box.id === history.undo[index].id)!;
        await port.remove(current.id);
        history.undo.splice(index);
        history.redo.push(rectOf(current));
      }
      if (command === 'redo' && history.redo.length > 0) {
        const created = await port.add(imageId, history.redo[history.redo.length - 1]);
        history.redo.pop();
        history.undo.push(created);
        return created;
      }
    } finally {
      this.busy = false;
    }
  }
}
