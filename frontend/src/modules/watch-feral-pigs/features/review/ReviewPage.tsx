import { useEffect, useMemo, useRef, useState, type ReactNode } from 'react';
import { Link, useParams, useSearchParams } from 'react-router-dom';
import {
  ChevronLeft,
  ChevronRight,
  Flag,
  Keyboard,
  Maximize,
  MousePointer2,
  Redo2,
  SquareDashed,
  SquareCheck,
  Undo2,
  ZoomIn,
  ZoomOut,
} from 'lucide-react';
import { followResize } from '@shared/imaging/annotations/geometry';
import { cameraName } from '../../domain/cameras';
import { TwinPane } from './TwinPane';
import { TwinCheckCard } from './TwinCheckCard';
import { useTwinCheck, useTwinMarkActions } from './twinHooks';
import {
  countedBoxAnnotations,
  markIdOfSighting,
  sightingId,
  sightingSuggestionAnnotations,
} from '../../adapters/annotations';
import type { Detection, ReviewImageFilter, TwinMark } from '../../api/types';
import { wfpApi } from '../../api/client';
import { Alert, Button, ButtonLink, Card, ErrorAlert, Kbd, SkeletonRows } from '@shared/controls';
import { ApiError } from '@shared/api/client';
import { cx, ntTime } from '@shared/lib/format';
import { wfpPath } from '../../paths';
import { SurveyStatusBadge } from '../../components';
import { useSurvey } from '../../api/queries/surveys';
import { BoxList } from './BoxList';
import { useDrawingHistory, useImageReview, useReviewImage, useReviewImages, useReviewSummary } from './hooks';
import { ImageComparison } from '@shared/imaging/comparison/ImageComparison';
import { ImageViewer, type ImageViewerHandle, type ViewerMode } from './ImageViewer';
import { isReviewFilter } from './filters';
import { PhotoStrip } from './PhotoStrip';
import { ReviewProgressCard } from './ReviewProgressCard';
import { StartReviewCard } from './StartReviewCard';
import { SHORTCUTS, useReviewShortcuts, type ShortcutAction } from './shortcuts';

/** Which photos the viewer shows when the photo has a twin from the other camera (D-039). */
type TwinView = 'both' | 'main' | 'twin';
const wideScreen = () => typeof window !== 'undefined' && window.matchMedia('(min-width: 768px)').matches;

/**
 * Ranger review tool (D-034): the photo with its boxes in a zoomable viewer, the photo strip below, and the box list,
 * survey progress and shortcuts on the right. The current photo and filter live in the URL (?photo=&filter=), so a
 * reload or a shared link opens the same place.
 */
export function ReviewPage() {
  const { id: surveyId = '' } = useParams();
  const [params, setParams] = useSearchParams();
  const photoId = params.get('photo') ?? undefined;

  const survey = useSurvey(surveyId);
  const summary = useReviewSummary(surveyId);
  // Default queue: what still needs checking; for a finished review, everything.
  const filterParam = params.get('filter');
  const filter: ReviewImageFilter = isReviewFilter(filterParam)
    ? filterParam
    : summary.data?.status === 'reviewed'
      ? 'all'
      : 'notReviewed';
  const queue = useReviewImages(surveyId, filter);
  const detail = useReviewImage(surveyId, photoId);
  const actions = useImageReview(surveyId, photoId);

  const viewer = useRef<ImageViewerHandle>(null);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [mode, setMode] = useState<ViewerMode>('select');
  const [showHelp, setShowHelp] = useState(false);
  const [notice, setNotice] = useState<string | null>(null);
  const twinViewer = useRef<ImageViewerHandle>(null);
  const [twinView, setTwinView] = useState<TwinView>(() => (wideScreen() ? 'both' : 'main'));

  const images = useMemo(() => queue.data ?? [], [queue.data]);
  const index = images.findIndex((i) => i.id === photoId);
  // Where the photo was in the queue, for when it drops out of the filter (e.g. "Not checked" after checking it).
  const lastIndex = useRef(0);
  if (index >= 0) lastIndex.current = index;
  const boxes = useMemo(() => detail.data?.detections ?? [], [detail.data]);
  const drawing = useDrawingHistory(surveyId, photoId, boxes);
  const photo = detail.data?.image;
  const twin = detail.data?.twin;
  const surveyCamera = survey.data?.camera ?? 'thermal';
  const showMain = !twin || twinView !== 'twin';
  const twinCheck = useTwinCheck(
    surveyId,
    photoId,
    detail.data?.detections ?? NO_BOXES,
    detail.dataUpdatedAt,
    Boolean(twin),
  );
  const twinActions = useTwinMarkActions(surveyId, photoId);
  // Pigs drawn on the twin first are suggested on this photo as dashed boxes (D-041); they aren't counted until confirmed.
  const twinName = twin ? cameraName(twin.camera).toLowerCase() : 'twin';
  const viewerAnnotations = useMemo(
    () => [...countedBoxAnnotations(boxes), ...sightingSuggestionAnnotations(twinCheck.data?.sightings, twinName)],
    [boxes, twinCheck.data, twinName],
  );
  /** A box on this photo moved or resized; a resize also resizes its placed box on the twin (D-041). */
  const changeCountedBox = (id: string, rect: { x: number; y: number; width: number; height: number }) => {
    const before = boxes.find((b) => b.id === id);
    actions.update.mutate({ id, body: rect });
    const mark = twinCheck.data?.marks.find((m) => m.detectionId === id && m.state === 'confirmed');
    if (!before || !mark || mark.x == null || mark.y == null || mark.width == null || mark.height == null) return;
    const partner = followResize(
      before,
      rect,
      { x: mark.x, y: mark.y, width: mark.width, height: mark.height },
      { w: twin?.widthPx, h: twin?.heightPx },
    );
    if (partner) twinActions.place(mark, partner);
  };
  /** A pig's box on the twin was placed or moved; resizing a placed one also resizes the counted box (D-041). */
  const changeTwinBox = (mark: TwinMark, rect: { x: number; y: number; width: number; height: number }) => {
    twinActions.place(mark, rect);
    const before =
      mark.state === 'confirmed' && mark.x != null && mark.y != null && mark.width != null && mark.height != null
        ? { x: mark.x, y: mark.y, width: mark.width, height: mark.height }
        : null;
    const counted = boxes.find((b) => b.id === mark.detectionId);
    if (!before || !counted) return;
    const partner = followResize(before, rect, counted, { w: photo?.widthPx, h: photo?.heightPx });
    if (partner) actions.update.mutate({ id: counted.id, body: partner });
  };
  /** Draw on the twin: places the selected pig if it isn't placed yet, else adds a sighting (D-041). */
  const drawOnTwin = (rect: { x: number; y: number; width: number; height: number }) => {
    if (!editable) return;
    const mark = twinCheck.data?.marks.find((m) => m.detectionId === selectedId && m.state !== 'confirmed');
    if (mark) twinActions.place(mark, rect);
    else
      twinActions.addSighting.mutate(rect, {
        onSuccess: (s) => {
          setSelectedId(sightingId(s.markId));
          setMode('select');
        },
      });
  };
  const status = summary.data?.status;
  const editable =
    status === 'inReview' &&
    !drawing.isPending &&
    !actions.update.isPending &&
    !actions.remove.isPending &&
    !actions.complete.isPending &&
    !actions.reopen.isPending;
  const reviewing = status === 'inReview' || status === 'reviewed';

  const go = (next: { photo?: string; filter?: ReviewImageFilter }) => {
    const p = new URLSearchParams(params);
    if (next.filter) p.set('filter', next.filter);
    if (next.photo) p.set('photo', next.photo);
    else if (next.filter) p.delete('photo');
    setParams(p, { replace: true });
  };

  // Open the first photo of the queue when none is chosen (or after changing the filter).
  useEffect(() => {
    if (!photoId && images.length > 0 && !queue.isPlaceholderData) go({ photo: images[0].id });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [photoId, images, queue.isPlaceholderData]);

  // New photo: select its first suggestion still to check, so A / R work straight away.
  useEffect(() => {
    setSelectedId((current) =>
      current && boxes.some((b) => b.id === current)
        ? current
        : (boxes.find((b) => b.status === 'pending')?.id ?? null),
    );
  }, [boxes]);
  useEffect(() => {
    setMode('select');
    setNotice(null);
  }, [photoId]);

  // Preload the next photo so moving on is instant.
  const nextImage = index >= 0 ? images[index + 1] : undefined;
  useEffect(() => {
    if (nextImage) new Image().src = wfpApi.surveyImageUrl(nextImage.id);
  }, [nextImage]);

  const selected = boxes.find((b) => b.id === selectedId) ?? null;
  const pendingHere = boxes.filter((b) => b.status === 'pending');

  const step = (delta: number) => {
    if (images.length === 0) return;
    // If the photo left the queue, the one now at its old position is already "next".
    const to = index >= 0 ? index + delta : delta > 0 ? lastIndex.current : lastIndex.current - 1;
    const target = images[Math.min(images.length - 1, Math.max(0, to))];
    if (target && target.id !== photoId) go({ photo: target.id });
  };

  const selectBox = (delta: number) => {
    if (boxes.length === 0) return;
    const i = boxes.findIndex((b) => b.id === selectedId);
    setSelectedId(boxes[(i + delta + boxes.length) % boxes.length].id);
  };

  const setStatus = (id: string, s: Detection['status']) => actions.update.mutate({ id, body: { status: s } });

  /** After a decision, move to the next box still to check on this photo. */
  const decide = (s: Detection['status']) => {
    const target = selected ?? pendingHere[0];
    if (!target || target.source === 'ranger') return;
    setStatus(target.id, s);
    const next = pendingHere.find((b) => b.id !== target.id);
    if (next) setSelectedId(next.id);
  };

  const checkAndNext = () => {
    if (!photoId || !editable) return;
    if (pendingHere.length > 0) {
      setNotice(
        `Accept or reject the ${pendingHere.length} box${pendingHere.length === 1 ? '' : 'es'} still to check first (A / R).`,
      );
      return;
    }
    // Work out where to go before the queue refreshes (the photo may leave a "Not checked" filter).
    const following = images[index + 1] ?? images[index - 1];
    actions.complete.mutate(undefined, {
      onSuccess: () => following && following.id !== photoId && go({ photo: following.id }),
    });
  };

  const onShortcut = (a: ShortcutAction) => {
    setNotice(null);
    switch (a) {
      case 'nextPhoto':
        return step(1);
      case 'prevPhoto':
        return step(-1);
      case 'nextBox':
        return selectBox(1);
      case 'prevBox':
        return selectBox(-1);
      case 'zoomIn':
        return viewer.current?.zoomBy(1.25);
      case 'zoomOut':
        return viewer.current?.zoomBy(0.8);
      case 'fit':
        viewer.current?.fit();
        return twinViewer.current?.fit();
      case 'help':
        return setShowHelp((h) => !h);
      case 'escape':
        setMode('select');
        return setSelectedId(null);
    }
    if (!editable) return;
    if (drawing.isBusy()) return;
    switch (a) {
      case 'undoDrawing':
        return showMain && drawing.run('undo');
      case 'redoDrawing':
        return showMain && drawing.run('redo', undefined, (d) => setSelectedId(d.id));
      case 'accept':
        return decide('accepted');
      case 'reject':
        return decide('rejected');
      case 'undo':
        return selected && selected.source === 'ai' && setStatus(selected.id, 'pending');
      case 'acceptAll':
        return pendingHere.forEach((b) => setStatus(b.id, 'accepted'));
      case 'delete':
        if (!selected) return;
        return selected.source === 'ranger' ? actions.remove.mutate(selected.id) : setStatus(selected.id, 'rejected');
      case 'draw':
        return setMode((m) => (m === 'draw' ? 'select' : 'draw'));
      case 'checkAndNext':
        return checkAndNext();
      case 'flag':
        return photo && actions.flag.mutate(!photo.flagged);
    }
  };
  useReviewShortcuts(onShortcut, reviewing);

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

  return (
    <>
      <header className="my-6 flex flex-wrap items-start justify-between gap-4">
        <div className="flex min-w-0 items-start gap-3">
          <Link
            to={wfpPath(`/surveys/${s.id}`)}
            aria-label="Back to the survey"
            className="mt-1 inline-flex size-9 shrink-0 items-center justify-center rounded-md text-gray-700 hover:bg-gray-200 focus:outline-none focus-visible:ring-focus dark:text-gray-300 dark:hover:bg-gray-700"
          >
            <ChevronLeft className="size-5" aria-hidden="true" />
          </Link>
          <div className="min-w-0">
            <h2 className="break-words text-2xl font-bold tracking-tight text-gray-900 sm:text-3xl dark:text-gray-100">
              <span className="font-semibold text-gray-500 dark:text-gray-400">Review: </span>
              {s.name}
            </h2>
            <div className="mt-2 flex flex-wrap items-center gap-x-4 gap-y-2 text-sm text-gray-600 dark:text-gray-400">
              <SurveyStatusBadge status={s.status} />
              <span>{s.siteName}</span>
              {summary.data && (
                <span>
                  {summary.data.reviewedPhotos} of {summary.data.photos} photos checked · {summary.data.confirmedPigs}{' '}
                  confirmed pig
                  {summary.data.confirmedPigs === 1 ? '' : 's'}
                </span>
              )}
            </div>
          </div>
        </div>
        {status === 'reviewed' && (
          <ButtonLink
            to={wfpPath(`/surveys/${s.id}/results`)}
            icon={<ChevronRight className="size-4" aria-hidden="true" />}
          >
            View results
          </ButtonLink>
        )}
      </header>

      {!reviewing ? (
        <StartReviewCard survey={s} />
      ) : (
        <div className="grid grid-cols-1 items-start gap-6 xl:grid-cols-[minmax(0,2fr)_minmax(20rem,1fr)]">
          <section aria-label="Photo" className="min-w-0 space-y-5">
            <Card bodyClassName="p-0!" className="overflow-hidden border-gray-200 dark:border-gray-700">
              {/* Navigation and editing tools stay separate from camera view selection. */}
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-gray-200 bg-gray-50/70 px-3 py-3 dark:border-gray-700 dark:bg-gray-900/30">
                <div className="flex min-w-0 flex-wrap items-center gap-3">
                  <div className="flex shrink-0 items-center rounded-lg border border-gray-200 bg-white dark:border-gray-600 dark:bg-gray-800">
                    <Button
                      variant="ghost"
                      className="min-h-11 rounded-r-none border-r border-gray-200! px-2! dark:border-gray-600!"
                      onClick={() => step(-1)}
                      disabled={images.length === 0 || index === 0}
                      aria-label="Previous photo (←)"
                      icon={<ChevronLeft size={16} aria-hidden="true" />}
                    >
                      <span className="hidden sm:inline">Prev</span>
                    </Button>
                    <p className="px-3 text-sm font-semibold text-gray-900 dark:text-gray-100" aria-live="polite">
                      {index >= 0
                        ? `Photo ${index + 1} of ${images.length}`
                        : queue.isLoading
                          ? 'Loading…'
                          : 'No photos'}
                    </p>
                    <Button
                      variant="ghost"
                      className="min-h-11 rounded-l-none border-l border-gray-200! px-2! dark:border-gray-600!"
                      onClick={() => step(1)}
                      disabled={images.length === 0 || index >= images.length - 1}
                      aria-label="Next photo (→)"
                    >
                      <span className="hidden sm:inline">Next</span>
                      <ChevronRight size={16} aria-hidden="true" />
                    </Button>
                  </div>
                  <p className="max-w-56 truncate text-xs text-gray-600 dark:text-gray-400" title={photo?.fileName}>
                    {photo?.fileName ?? (queue.isLoading ? 'Loading photo…' : 'Choose a photo')}
                    {photo?.takenAt && <span className="ml-2">{ntTime(photo.takenAt)}</span>}
                  </p>
                </div>
                <div className="flex flex-wrap items-center gap-1" role="group" aria-label="Photo editing tools">
                  <ToolButton
                    active={mode === 'select'}
                    onClick={() => setMode('select')}
                    icon={<MousePointer2 size={16} aria-hidden="true" />}
                    label="Select"
                  />
                  <ToolButton
                    active={mode === 'draw'}
                    disabled={!editable}
                    onClick={() => setMode('draw')}
                    icon={<SquareDashed size={16} aria-hidden="true" />}
                    label="Draw box (N)"
                  />
                  <span className="mx-1 h-6 w-px bg-gray-200 dark:bg-gray-600" aria-hidden="true" />
                  <Button
                    variant="ghost"
                    className="size-11 p-0!"
                    disabled={!editable || !showMain || !drawing.canUndo}
                    aria-label="Undo drawing (Ctrl or Cmd + Z)"
                    title="Undo drawing (Ctrl/Cmd + Z)"
                    onClick={() => drawing.run('undo')}
                    icon={<Undo2 size={16} aria-hidden="true" />}
                  />
                  <Button
                    variant="ghost"
                    className="size-11 p-0!"
                    disabled={!editable || !showMain || !drawing.canRedo}
                    aria-label="Redo drawing (Ctrl or Cmd + Y)"
                    title="Redo drawing (Ctrl/Cmd + Y)"
                    onClick={() => drawing.run('redo', undefined, (d) => setSelectedId(d.id))}
                    icon={<Redo2 size={16} aria-hidden="true" />}
                  />
                  <Button
                    variant="ghost"
                    className="size-11 p-0!"
                    aria-label="Zoom out (−)"
                    title="Zoom out"
                    onClick={() => viewer.current?.zoomBy(0.8)}
                    icon={<ZoomOut size={16} aria-hidden="true" />}
                  />
                  <Button
                    variant="ghost"
                    className="size-11 p-0!"
                    aria-label="Zoom in (+)"
                    title="Zoom in"
                    onClick={() => viewer.current?.zoomBy(1.25)}
                    icon={<ZoomIn size={16} aria-hidden="true" />}
                  />
                  <Button
                    variant="ghost"
                    className="size-11 p-0!"
                    aria-label="Fit photo (0)"
                    title="Fit photos"
                    onClick={() => {
                      viewer.current?.fit();
                      twinViewer.current?.fit();
                    }}
                    icon={<Maximize size={16} aria-hidden="true" />}
                  />
                </div>
              </div>
              <ImageComparison
                name="review-camera-view"
                layout="workspace"
                view={twinView}
                onView={(value) => setTwinView(value as TwinView)}
                panes={[
                  {
                    id: 'main',
                    label: cameraName(surveyCamera),
                    content: (
                      <>
                        {photoId ? (
                          <ViewerPanel label={twin ? `${cameraName(surveyCamera)} · counted` : undefined}>
                            <ImageViewer
                              key={photoId}
                              ref={viewer}
                              src={wfpApi.surveyImageUrl(photoId)}
                              alt={photo?.fileName ?? 'Survey photo'}
                              annotations={viewerAnnotations}
                              selectedId={selectedId}
                              onSelect={setSelectedId}
                              mode={mode}
                              editable={editable}
                              onCreate={(rect) => {
                                if (editable)
                                  drawing.run('draw', rect, (d) => {
                                    setSelectedId(d.id);
                                    setMode('select');
                                  });
                              }}
                              onChangeBox={(id, rect) => {
                                // Moving a suggestion from the twin confirms it here (it becomes a counted pig, D-041).
                                const sighting = twinCheck.data?.sightings.find(
                                  (s) => s.markId === markIdOfSighting(id),
                                );
                                if (sighting) twinActions.confirmOnCounted(sighting, rect);
                                else changeCountedBox(id, rect);
                              }}
                            />
                          </ViewerPanel>
                        ) : !photoId ? (
                          <div className="grid size-full place-items-center bg-gray-900 text-sm text-gray-300">
                            {queue.isLoading ? 'Loading photos…' : 'Choose a photo below.'}
                          </div>
                        ) : null}
                      </>
                    ),
                  },
                  ...(twin
                    ? [
                        {
                          id: 'twin',
                          label: cameraName(twin.camera),
                          content: (
                            <ViewerPanel label={`${cameraName(twin.camera)} · reference, not counted`} muted>
                              <TwinPane
                                ref={twinViewer}
                                twin={twin}
                                marks={twinCheck.data?.marks ?? []}
                                sightings={twinCheck.data?.sightings ?? []}
                                selectedId={selectedId}
                                onSelect={setSelectedId}
                                mode={mode}
                                editable={editable}
                                onPlace={changeTwinBox}
                                onDraw={drawOnTwin}
                              />
                            </ViewerPanel>
                          ),
                        },
                      ]
                    : []),
                ]}
              />
              {twin && twinView === 'twin' && (
                <p className="border-t border-gray-200 px-3 py-2 text-xs text-gray-600 dark:border-gray-700 dark:text-gray-400">
                  Reference photo {twin.fileName}. Only confirmed boxes on the {cameraName(surveyCamera).toLowerCase()}{' '}
                  photo contribute to the count.
                </p>
              )}

              {/* Photo actions */}
              {photo && (
                <div className="flex flex-wrap items-center gap-3 border-t border-gray-200 p-4 dark:border-gray-700">
                  {photo.reviewed ? (
                    <>
                      <span className="inline-flex items-center gap-1.5 text-sm text-green-700 dark:text-green-300">
                        <SquareCheck className="size-4" aria-hidden="true" />
                        Checked{photo.reviewedBy ? ` by ${photo.reviewedBy}` : ''}
                      </span>
                      {editable && (
                        <Button
                          variant="outline"
                          size="sm"
                          className="min-h-10"
                          icon={<Undo2 className="size-4" aria-hidden="true" />}
                          loading={actions.reopen.isPending}
                          onClick={() => actions.reopen.mutate()}
                        >
                          Mark not checked
                        </Button>
                      )}
                      <Button
                        variant="ghost"
                        size="sm"
                        className="min-h-10"
                        onClick={() => step(1)}
                        disabled={index >= images.length - 1}
                      >
                        Next photo
                      </Button>
                    </>
                  ) : (
                    editable && (
                      <Button
                        icon={<SquareCheck className="size-4" aria-hidden="true" />}
                        loading={actions.complete.isPending}
                        onClick={checkAndNext}
                      >
                        {boxes.some((b) => b.status === 'accepted') ? 'Photo checked, next' : 'No (more) pigs, next'}{' '}
                        <Kbd>Space</Kbd>
                      </Button>
                    )
                  )}
                  {editable && (
                    <Button
                      variant="outline"
                      size="sm"
                      className={cx('min-h-10', photo.flagged && 'border-red-400! text-red-700! dark:text-red-300!')}
                      aria-pressed={photo.flagged}
                      icon={<Flag className="size-4" aria-hidden="true" />}
                      onClick={() => actions.flag.mutate(!photo.flagged)}
                    >
                      {photo.flagged ? 'Flagged' : 'Flag (F)'}
                    </Button>
                  )}
                  <span className="ml-auto rounded-md border border-gray-100 bg-gray-50 px-3 py-2 font-mono text-xs text-gray-600 dark:border-gray-700 dark:bg-gray-900 dark:text-gray-400">
                    {photo.latitude != null && photo.longitude != null
                      ? `GPS ${photo.latitude.toFixed(5)}, ${photo.longitude.toFixed(5)}`
                      : 'No GPS'}
                  </span>
                </div>
              )}

              {(notice || mutationError) && (
                <div className="px-3 pb-3" aria-live="assertive">
                  {notice ? (
                    <Alert tone="warning">{notice}</Alert>
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
              images={images}
              currentId={photoId}
              filter={filter}
              onFilter={(f) => go({ filter: f })}
              onOpen={(id) => go({ photo: id })}
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
                  selectedId={selectedId}
                  editable={editable}
                  onSelect={setSelectedId}
                  onStatus={setStatus}
                  onDelete={(id) => actions.remove.mutate(id)}
                />
              )}
              {editable && pendingHere.length > 1 && (
                <Button
                  className="mt-3"
                  size="sm"
                  variant="outline"
                  onClick={() => pendingHere.forEach((b) => setStatus(b.id, 'accepted'))}
                >
                  Accept all {pendingHere.length} on this photo
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
                selectedId={selectedId}
                onSelect={setSelectedId}
                onPlace={(m) => twinActions.place(m)}
                onNotVisible={twinActions.notVisible}
                onReset={(m) => twinActions.clear.mutate(m.detectionId)}
                onConfirmSighting={(s) => twinActions.confirmOnCounted(s)}
                onSightingNotVisible={(s, notVisible) =>
                  twinActions.sightingVisibility.mutate({ markId: s.markId, notVisible })
                }
                onRemoveSighting={(s) => twinActions.removeSighting.mutate(s.markId)}
              />
            )}

            {summary.data && <ReviewProgressCard summary={summary.data} />}

            <Card
              title={
                <span className="inline-flex items-center gap-2">
                  <Keyboard className="size-4" aria-hidden="true" /> Keyboard shortcuts
                </span>
              }
              actions={
                <Button variant="link" size="sm" aria-expanded={showHelp} onClick={() => setShowHelp((h) => !h)}>
                  {showHelp ? 'Hide' : 'Show'}
                </Button>
              }
            >
              {showHelp ? (
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
                  <Kbd>A</Kbd> accept · <Kbd>R</Kbd> reject · <Kbd>N</Kbd> draw · <Kbd>Space</Kbd> next. Press{' '}
                  <Kbd>?</Kbd> for all.
                </p>
              )}
            </Card>
          </aside>
        </div>
      )}
    </>
  );
}

const NO_BOXES: Detection[] = [];

/** One photo pane; a small label says which camera it is and whether it counts (D-039). */
function ViewerPanel({ label, muted = false, children }: { label?: string; muted?: boolean; children: ReactNode }) {
  return (
    <div className="relative size-full min-w-0">
      {children}
      {label && (
        <span
          className={cx(
            'pointer-events-none absolute left-2 top-2 z-10 rounded-md px-2 py-1 text-xs font-semibold shadow',
            muted ? 'bg-gray-900/80 text-gray-100' : 'bg-brand-600 text-white',
          )}
        >
          {label}
        </span>
      )}
    </div>
  );
}

function ToolButton({
  active,
  disabled,
  onClick,
  icon,
  label,
}: {
  active: boolean;
  disabled?: boolean;
  onClick(): void;
  icon?: ReactNode;
  label: string;
}) {
  return (
    <Button
      variant={active ? 'primary' : 'outline'}
      disabled={disabled}
      onClick={onClick}
      icon={icon}
      aria-pressed={active}
      title={label}
      className="min-h-11 px-3!"
    >
      {label}
    </Button>
  );
}
