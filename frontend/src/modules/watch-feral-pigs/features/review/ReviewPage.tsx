import { useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Alert, Button, Card, ErrorAlert, SkeletonRows } from '@shared/controls';
import { ApiError } from '@shared/api/client';
import { ImageComparison, type ComparisonView } from '@shared/imaging/comparison/ImageComparison';
import { wfpApi } from '../../api/client';
import { useSurvey } from '../../api/queries/surveys';
import { cameraName } from '../../domain/cameras';
import { BoxList } from './BoxList';
import { useDrawingHistory, useReviewImage, useReviewSummary } from './hooks';
import { ImageViewer, type ImageViewerHandle, type ViewerMode } from './ImageViewer';
import { PhotoActions } from './PhotoActions';
import { PhotoStrip } from './PhotoStrip';
import { ReviewHeader } from './ReviewHeader';
import { ReviewProgressCard } from './ReviewProgressCard';
import { ReviewToolbar } from './ReviewToolbar';
import { ShortcutHelpCard } from './ShortcutHelpCard';
import { useReviewShortcuts, type ShortcutAction } from './shortcuts';
import { StartReviewCard } from './StartReviewCard';
import { TwinCheckCard } from './TwinCheckCard';
import { TwinPane } from './TwinPane';
import { useBoxSelection } from './useBoxSelection';
import { useReviewCommands } from './useReviewCommands';
import { useReviewQueue } from './useReviewQueue';
import { useTwinCoordination } from './useTwinCoordination';
import { ViewerPanel } from './ViewerPanel';

/** Which photos the viewer shows when the photo has a twin from the other camera (D-039). */
type PaneId = 'main' | 'twin';
const wideScreen = () => typeof window !== 'undefined' && window.matchMedia('(min-width: 768px)').matches;
type Keymap = Partial<Record<ShortcutAction, () => unknown>>;

/**
 * Ranger review tool (D-034): the photo with its boxes in a zoomable viewer, the photo strip below, and the box list,
 * twin check, survey progress and shortcuts on the right. This page only wires things together (docs/18 step 4):
 * the queue, selection, commands and twin logic live in the `use…` hooks next to it.
 */
export function ReviewPage() {
  const { id: surveyId = '' } = useParams();
  const survey = useSurvey(surveyId);
  const summary = useReviewSummary(surveyId);
  const status = summary.data?.status;
  const queue = useReviewQueue(surveyId, status);
  const { photoId } = queue;
  const detail = useReviewImage(surveyId, photoId);
  const photo = detail.data?.image;
  const boxes = useMemo(() => detail.data?.detections ?? [], [detail.data]);

  const viewer = useRef<ImageViewerHandle>(null);
  const twinViewer = useRef<ImageViewerHandle>(null);
  const [mode, setMode] = useState<ViewerMode>('select');
  const [showHelp, setShowHelp] = useState(false);
  const [view, setView] = useState<ComparisonView<PaneId>>(() => (wideScreen() ? 'both' : 'main'));
  useEffect(() => setMode('select'), [photoId]);

  const selection = useBoxSelection(boxes);
  const select = selection.setSelectedId;
  const drawing = useDrawingHistory(surveyId, photoId, boxes);
  const commands = useReviewCommands({
    surveyId,
    photoId,
    photo,
    status,
    boxes,
    selected: selection.selected,
    select,
    drawing,
    onChecked: () => {
      const next = queue.following;
      if (next && next.id !== photoId) queue.open(next.id);
    },
  });
  const { actions, editable } = commands;
  const twins = useTwinCoordination({
    surveyId,
    photoId,
    detail: detail.data,
    detailUpdatedAt: detail.dataUpdatedAt,
    boxes,
    editable,
    selectedId: selection.selectedId,
    select,
    updateBox: (id, rect) => actions.update.mutate({ id, body: rect }),
    onDrawn: () => setMode('select'),
  });
  const { twin, twinCheck, twinActions } = twins;
  const surveyCamera = survey.data?.camera ?? 'thermal';
  // Drawing undo/redo applies to the counted photo, so only while it is on screen.
  const showMain = !twin || view !== 'twin';
  const reviewing = status === 'inReview' || status === 'reviewed';

  const fit = () => {
    viewer.current?.fit();
    twinViewer.current?.fit();
  };
  const undoDrawing = () => drawing.run('undo');
  const redoDrawing = () => drawing.run('redo', undefined, (d) => select(d.id));

  // ---- keyboard: viewing keys always work; editing keys only while the photo can be edited ----
  const viewingKeys: Keymap = {
    nextPhoto: () => queue.step(1),
    prevPhoto: () => queue.step(-1),
    nextBox: () => selection.selectBox(1),
    prevBox: () => selection.selectBox(-1),
    zoomIn: () => viewer.current?.zoomBy(1.25),
    zoomOut: () => viewer.current?.zoomBy(0.8),
    fit,
    help: () => setShowHelp((h) => !h),
    escape: () => {
      setMode('select');
      select(null);
    },
  };
  const editingKeys: Keymap = {
    undoDrawing: () => showMain && undoDrawing(),
    redoDrawing: () => showMain && redoDrawing(),
    accept: () => commands.decide('accepted'),
    reject: () => commands.decide('rejected'),
    undo: commands.undoDecision,
    acceptAll: commands.acceptAll,
    delete: commands.deleteSelected,
    draw: () => setMode((m) => (m === 'draw' ? 'select' : 'draw')),
    checkAndNext: commands.checkAndNext,
    flag: commands.toggleFlag,
  };
  useReviewShortcuts((action) => {
    commands.setNotice(null);
    const run = viewingKeys[action] ?? (editable && !drawing.isBusy() ? editingKeys[action] : undefined);
    run?.();
  }, reviewing);

  const mutationError =
    drawing.error ??
    [
      actions.update,
      actions.add,
      actions.remove,
      actions.complete,
      actions.reopen,
      actions.flag,
      ...twinActions.mutations,
    ].find((m) => m.isError)?.error;

  // ---- render ----

  if (survey.isLoading || summary.isLoading)
    return (
      <div className="my-6">
        <SkeletonRows rows={8} />
      </div>
    );
  if (survey.isError)
    return (
      <div className="my-6">
        <ErrorAlert error={survey.error} title="Couldn't load this survey" />
      </div>
    );
  const s = survey.data;
  if (!s) return null;

  const mainPane = photoId ? (
    <ViewerPanel label={twin ? `${cameraName(surveyCamera)} · counted` : undefined}>
      <ImageViewer
        key={photoId}
        ref={viewer}
        src={wfpApi.surveyImageUrl(photoId)}
        alt={photo?.fileName ?? 'Survey photo'}
        annotations={twins.countedAnnotations}
        selectedId={selection.selectedId}
        onSelect={select}
        mode={mode}
        editable={editable}
        onCreate={(rect) => {
          if (editable)
            drawing.run('draw', rect, (d) => {
              select(d.id);
              setMode('select');
            });
        }}
        onChangeBox={twins.changeCountedBox}
      />
    </ViewerPanel>
  ) : (
    <div className="grid size-full place-items-center bg-gray-900 text-sm text-gray-300">
      {queue.isLoading ? 'Loading photos…' : 'Choose a photo below.'}
    </div>
  );

  const twinPane = twin && (
    <ViewerPanel label={`${cameraName(twin.camera)} · reference, not counted`} muted>
      <TwinPane
        ref={twinViewer}
        twin={twin}
        marks={twinCheck.data?.marks ?? []}
        sightings={twinCheck.data?.sightings ?? []}
        selectedId={selection.selectedId}
        onSelect={select}
        mode={mode}
        editable={editable}
        onPlace={twins.changeTwinBox}
        onDraw={twins.drawOnTwin}
      />
    </ViewerPanel>
  );

  return (
    <>
      <ReviewHeader survey={s} summary={summary.data} />

      {!reviewing ? (
        <StartReviewCard survey={s} />
      ) : (
        <div className="grid grid-cols-1 items-start gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(20rem,1fr)]">
          <section aria-label="Photo" className="min-w-0 space-y-5">
            <Card bodyClassName="p-0!" className="overflow-hidden border-gray-200 dark:border-gray-700">
              <ReviewToolbar
                position={
                  queue.index >= 0
                    ? `Photo ${queue.index + 1} of ${queue.images.length}`
                    : queue.isLoading
                      ? 'Loading…'
                      : 'No photos'
                }
                canPrev={queue.canPrev}
                canNext={queue.canNext}
                onPrev={() => queue.step(-1)}
                onNext={() => queue.step(1)}
                fileName={photo?.fileName}
                takenAt={photo?.takenAt}
                loading={queue.isLoading}
                mode={mode}
                onMode={setMode}
                editable={editable}
                canUndo={editable && showMain && drawing.canUndo}
                canRedo={editable && showMain && drawing.canRedo}
                onUndo={undoDrawing}
                onRedo={redoDrawing}
                onZoom={(factor) => viewer.current?.zoomBy(factor)}
                onFit={fit}
              />
              <ImageComparison<PaneId>
                name="review-camera-view"
                layout="workspace"
                view={view}
                onView={setView}
                panes={[
                  { id: 'main', label: cameraName(surveyCamera), content: mainPane },
                  ...(twin ? [{ id: 'twin' as const, label: cameraName(twin.camera), content: twinPane }] : []),
                ]}
              />
              {twin && view === 'twin' && (
                <p className="border-t border-gray-200 px-3 py-2 text-xs text-gray-600 dark:border-gray-700 dark:text-gray-400">
                  Reference photo {twin.fileName}. Only confirmed boxes on the {cameraName(surveyCamera).toLowerCase()}{' '}
                  photo contribute to the count.
                </p>
              )}

              {photo && (
                <PhotoActions
                  photo={photo}
                  editable={editable}
                  hasConfirmedPigs={boxes.some((b) => b.status === 'accepted')}
                  canNext={queue.canNext}
                  completing={actions.complete.isPending}
                  reopening={actions.reopen.isPending}
                  onCheckAndNext={commands.checkAndNext}
                  onReopen={() => actions.reopen.mutate()}
                  onNext={() => queue.step(1)}
                  onToggleFlag={commands.toggleFlag}
                />
              )}

              {(commands.notice || mutationError) && (
                <div className="px-3 pb-3" aria-live="assertive">
                  {commands.notice ? (
                    <Alert tone="warning">{commands.notice}</Alert>
                  ) : (
                    <Alert tone="danger">
                      {mutationError instanceof ApiError
                        ? mutationError.message
                        : 'That change was not saved. Please try again.'}
                    </Alert>
                  )}
                </div>
              )}
            </Card>
            <PhotoStrip
              images={queue.images}
              currentId={photoId}
              filter={queue.filter}
              onFilter={queue.setFilter}
              onOpen={queue.open}
              loading={queue.isFetching}
            />
          </section>

          <aside className="min-w-0 space-y-5">
            <Card
              title={`Boxes on this photo (${boxes.length})`}
              description={editable ? 'Select a box to highlight it on the photo.' : undefined}
            >
              {detail.isLoading ? (
                <SkeletonRows rows={3} />
              ) : (
                <BoxList
                  boxes={boxes}
                  selectedId={selection.selectedId}
                  editable={editable}
                  onSelect={select}
                  onStatus={commands.setStatus}
                  onDelete={(id) => actions.remove.mutate(id)}
                />
              )}
              {editable && commands.pending.length > 1 && (
                <Button className="mt-3" size="sm" variant="outline" onClick={commands.acceptAll}>
                  Accept all {commands.pending.length} on this photo
                </Button>
              )}
            </Card>

            {twin && (
              <TwinCheckCard
                twin={twin}
                countedCamera={surveyCamera}
                check={twinCheck.data}
                loading={twinCheck.isLoading}
                editable={editable}
                selectedId={selection.selectedId}
                onSelect={select}
                onPlace={(m) => twinActions.place(m)}
                onNotVisible={twinActions.notVisible}
                onReset={(m) => twinActions.clear.mutate(m.detectionId)}
                onConfirmSighting={(sighting) => twinActions.confirmOnCounted(sighting)}
                onSightingNotVisible={(sighting, notVisible) =>
                  twinActions.sightingVisibility.mutate({ markId: sighting.markId, notVisible })
                }
                onRemoveSighting={(sighting) => twinActions.removeSighting.mutate(sighting.markId)}
              />
            )}

            {summary.data && <ReviewProgressCard summary={summary.data} />}

            <ShortcutHelpCard open={showHelp} onToggle={() => setShowHelp((h) => !h)} />
          </aside>
        </div>
      )}
    </>
  );
}
