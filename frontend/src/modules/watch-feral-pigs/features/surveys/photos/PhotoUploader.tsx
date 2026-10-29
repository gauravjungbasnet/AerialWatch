import { useRef, useState, type DragEvent, type KeyboardEvent } from 'react';
import { FolderOpen, UploadCloud } from 'lucide-react';
import { Button, EmptyState } from '../../../../../shared/controls';
import { cx } from '../../../../../shared/lib/format';
import { ACCEPT, FILES_PER_BATCH } from './uploadRules';
import { UploadResultsTable } from './UploadResultsTable';
import { useUploadPhotos } from './useUploadPhotos';

// Lets the folder picker through React's typed props (supported by Chrome, Edge, Firefox and Safari).
const folderPickerProps = { webkitdirectory: '', directory: '' } as Record<string, string>;

type Tab = 'upload' | 'session';

/**
 * Upload card with two tabs: the drop zone, and what happened to every file sent since the page opened.
 * Photos go up in batches of 20; when an upload finishes the card switches to the session tab.
 */
export function PhotoUploader({ surveyId }: { surveyId: string }) {
  const { progress, start, cancel, clear } = useUploadPhotos(surveyId);
  const [tab, setTab] = useState<Tab>('upload');
  const [dragging, setDragging] = useState(false);
  const filesInput = useRef<HTMLInputElement>(null);
  const folderInput = useRef<HTMLInputElement>(null);

  const pick = async (list: FileList | null) => {
    if (!list || list.length === 0 || progress.running) return;
    await start(Array.from(list));
    setTab('session');
  };

  const onDrop = (e: DragEvent) => {
    e.preventDefault();
    setDragging(false);
    void pick(e.dataTransfer.files);
  };

  const percentDone = progress.total > 0 ? Math.round((progress.done / progress.total) * 100) : 0;
  const tabs: { id: Tab; label: string }[] = [
    { id: 'upload', label: 'Upload new photos' },
    { id: 'session', label: `Session uploads (${progress.results.length})` },
  ];

  const onTabKey = (e: KeyboardEvent) => {
    if (e.key !== 'ArrowRight' && e.key !== 'ArrowLeft') return;
    setTab((t) => (t === 'upload' ? 'session' : 'upload'));
  };

  return (
    <div className="overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm dark:border-gray-700 dark:bg-gray-800">
      <div
        role="tablist"
        aria-label="Photo upload"
        className="flex overflow-x-auto border-b border-gray-200 dark:border-gray-700"
        onKeyDown={onTabKey}
      >
        {tabs.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            id={`photos-tab-${t.id}`}
            aria-controls={`photos-panel-${t.id}`}
            aria-selected={tab === t.id}
            tabIndex={tab === t.id ? 0 : -1}
            onClick={() => setTab(t.id)}
            className={cx(
              '-mb-px whitespace-nowrap border-b-2 px-6 py-3.5 text-sm transition-colors focus:outline-none focus-visible:ring-focus',
              tab === t.id
                ? 'border-brand-600 font-semibold text-brand-700 dark:text-brand-300'
                : 'border-transparent font-medium text-gray-500 hover:text-gray-700 dark:text-gray-400 dark:hover:text-gray-200',
            )}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div
        role="tabpanel"
        id="photos-panel-upload"
        aria-labelledby="photos-tab-upload"
        hidden={tab !== 'upload'}
        className="p-6"
      >
        <div
          onDragOver={(e) => {
            e.preventDefault();
            if (!progress.running) setDragging(true);
          }}
          onDragLeave={() => setDragging(false)}
          onDrop={onDrop}
          className={cx(
            'flex flex-col items-center justify-center rounded-lg border-2 border-dashed p-8 text-center transition-colors',
            dragging
              ? 'border-brand-500 bg-brand-50'
              : 'border-brand-200 bg-brand-50/50 hover:bg-brand-50 dark:border-gray-600 dark:bg-gray-900/40',
          )}
        >
          <div className="mx-auto mb-4 flex size-12 items-center justify-center rounded-full border border-brand-100 bg-white text-brand-600 shadow-sm dark:border-gray-600 dark:bg-gray-800">
            <UploadCloud className="size-6" aria-hidden="true" />
          </div>
          <p className="mb-1 text-base font-semibold text-gray-800 dark:text-gray-100">
            Drag drone photos here, or choose files
          </p>
          <p className="mb-6 text-xs text-gray-500 dark:text-gray-400">
            JPEG or PNG, up to 50 MB each. Sent {FILES_PER_BATCH} at a time; the same photo is only stored once.
          </p>
          <div className="flex w-full flex-wrap justify-center gap-3 sm:w-auto">
            <Button
              className="w-full sm:w-auto"
              onClick={() => filesInput.current?.click()}
              disabled={progress.running}
            >
              Choose photos
            </Button>
            <Button
              variant="outline"
              className="w-full sm:w-auto"
              icon={<FolderOpen className="size-4" aria-hidden="true" />}
              onClick={() => folderInput.current?.click()}
              disabled={progress.running}
            >
              Choose folder
            </Button>
          </div>
          <input
            ref={filesInput}
            type="file"
            accept={ACCEPT}
            multiple
            className="sr-only"
            aria-label="Choose photos to upload"
            onChange={(e) => {
              void pick(e.target.files);
              e.target.value = ''; // allow picking the same files again
            }}
          />
          <input
            ref={folderInput}
            type="file"
            multiple
            className="sr-only"
            aria-label="Choose a folder of photos to upload"
            {...folderPickerProps}
            onChange={(e) => {
              void pick(e.target.files);
              e.target.value = '';
            }}
          />
        </div>

        {progress.running && (
          <div role="status" aria-live="polite" className="mt-4">
            <div className="mb-1 flex items-center justify-between text-sm text-gray-700 dark:text-gray-300">
              <span>
                Uploading {progress.done} of {progress.total} photos…
              </span>
              <Button variant="ghost" size="sm" onClick={cancel}>
                Cancel
              </Button>
            </div>
            <div className="h-2 w-full overflow-hidden rounded-full bg-gray-200 dark:bg-gray-700">
              <div className="h-full rounded-full bg-brand-600 transition-all" style={{ width: `${percentDone}%` }} />
            </div>
          </div>
        )}
      </div>

      <div
        role="tabpanel"
        id="photos-panel-session"
        aria-labelledby="photos-tab-session"
        hidden={tab !== 'session'}
        className="p-6"
      >
        {progress.results.length === 0 && !progress.error && !progress.cancelled ? (
          <EmptyState
            title="Nothing uploaded yet"
            description="Files you upload while this page is open are listed here with what was read from each."
          />
        ) : (
          <UploadResultsTable
            results={progress.results}
            error={progress.error}
            cancelled={progress.cancelled}
            onClose={() => {
              clear();
              setTab('upload');
            }}
          />
        )}
      </div>
    </div>
  );
}
