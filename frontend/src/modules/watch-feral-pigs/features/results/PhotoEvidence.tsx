import { useState } from 'react';
import { ExternalLink, Info } from 'lucide-react';
import { wfpApi } from '../../api/client';
import type { CameraType, Detection, ReviewImageDetail, TwinCheck } from '../../api/types';
import { Alert, Badge, Button, ButtonLink, Modal, ErrorAlert, SkeletonRows } from '@shared/controls';
import { ntTime, percent } from '@shared/lib/format';
import { cameraName } from '../../domain/cameras';
import { wfpPath } from '../../paths';
import { boxStyle } from '../../domain/boxAppearance';
import { usePhotoEvidence } from './hooks';
import { ImageComparison } from '@shared/imaging/comparison/ImageComparison';
import { AnnotatedImage, type ImageAnnotation } from '@shared/imaging/viewer/AnnotatedImage';

const twinColour = 'var(--color-violet-400)';

function countedMarks(boxes: Detection[]): ImageAnnotation[] {
  // Same numbers as the review tool: position in the photo's full box list.
  return boxes.flatMap((b, i) =>
    b.status !== 'accepted'
      ? []
      : [
          {
            key: b.id,
            x: b.x,
            y: b.y,
            width: b.width,
            height: b.height,
            color: boxStyle(b).color,
            label: String(i + 1),
          },
        ],
  );
}

function twinMarks(check: TwinCheck | undefined): ImageAnnotation[] {
  if (!check) return [];
  const placed = check.marks.flatMap((m) =>
    m.state === 'confirmed' && m.x != null && m.y != null && m.width != null && m.height != null
      ? [
          {
            key: m.detectionId,
            x: m.x,
            y: m.y,
            width: m.width,
            height: m.height,
            label: String(m.number),
            color: twinColour,
          },
        ]
      : [],
  );
  const onlyHere = check.sightings
    .filter((s) => s.state === 'notVisibleOnCounted')
    .map((s) => ({
      key: s.markId,
      x: s.twinX,
      y: s.twinY,
      width: s.twinWidth,
      height: s.twinHeight,
      label: s.label,
      color: 'var(--color-amber-400)',
      dashed: true,
    }));
  return [...placed, ...onlyHere];
}

type View = 'both' | 'counted' | 'twin';

function pigOnTwin(state: string | undefined, twinName: string) {
  if (state === 'confirmed') return `seen on ${twinName} too`;
  if (state === 'notVisible') return `not visible on ${twinName}`;
  return `not checked on ${twinName}`;
}

/** Selected map dot: the counted photo and its twin with confirmed pigs marked, plus what was found and how. */
export function PhotoEvidence({
  surveyId,
  imageId,
  camera,
}: {
  surveyId: string;
  imageId: string;
  camera: CameraType;
}) {
  const { detail, twin } = usePhotoEvidence(surveyId, imageId);
  const [view, setView] = useState<View>('both');

  if (detail.isLoading) return <SkeletonRows rows={4} />;
  if (detail.isError) return <ErrorAlert error={detail.error} title="Couldn't load this photo" />;
  if (!detail.data) return null;
  return (
    <Evidence
      surveyId={surveyId}
      camera={camera}
      data={detail.data}
      check={twin.data}
      twinError={twin.isError}
      view={view}
      onView={setView}
    />
  );
}

function Evidence({
  surveyId,
  camera,
  data,
  check,
  twinError,
  view,
  onView,
}: {
  surveyId: string;
  camera: CameraType;
  data: ReviewImageDetail;
  check?: TwinCheck;
  twinError: boolean;
  view: View;
  onView: (v: View) => void;
}) {
  const [showInfo, setShowInfo] = useState(false);
  const { image, detections, twin } = data;
  const accepted = detections.filter((d) => d.status === 'accepted');
  const byAi = accepted.filter((d) => d.source === 'ai').length;
  const byRanger = accepted.length - byAi;
  const rejected = detections.filter((d) => d.status === 'rejected').length;
  const pending = detections.filter((d) => d.status === 'pending').length;
  const countedCamera = camera;
  const twinName = twin ? cameraName(twin.camera).toLowerCase() : '';
  const markOf = new Map(check?.marks.map((m) => [m.detectionId, m]) ?? []);
  const seenOnBoth = accepted.filter((d) => markOf.get(d.id)?.state === 'confirmed').length;
  const notOnTwin = accepted.filter((d) => markOf.get(d.id)?.state === 'notVisible').length;
  const onlyOnTwin = check?.sightings.filter((s) => s.state === 'notVisibleOnCounted').length ?? 0;

  return (
    <div className="min-w-0 space-y-4">
      <ImageComparison
        name={`evidence-view-${image.id}`}
        view={view}
        onView={(value) => onView(value as View)}
        actions={
          <div
            className="ml-auto flex flex-wrap items-center gap-4"
            aria-label="Photo review status and confirmed count"
          >
            <Badge tone={image.reviewed ? 'success' : 'warning'}>
              {image.reviewed ? 'Checked' : 'Not checked yet'}
            </Badge>
            {image.flagged && <Badge tone="danger">Flagged</Badge>}
            <p className="flex items-baseline gap-2">
              <span className="text-3xl font-bold tabular-nums text-gray-900 dark:text-gray-100">
                {accepted.length}
              </span>
              <span className="text-sm font-semibold text-gray-500 dark:text-gray-400">
                {accepted.length === 1 ? 'pig' : 'pigs'}
              </span>
            </p>
            <Button
              variant="ghost"
              className="size-11 shrink-0 p-0!"
              aria-label="Photo findings and details"
              title="Photo findings and details"
              aria-haspopup="dialog"
              onClick={() => setShowInfo(true)}
            >
              <Info className="size-5" aria-hidden="true" />
            </Button>
          </div>
        }
        panes={[
          {
            id: 'counted',
            label: cameraName(countedCamera),
            content: (
              <AnnotatedImage
                src={wfpApi.surveyImageUrl(image.id)}
                alt={`Counted photo ${image.fileName} with ${accepted.length} confirmed pigs marked`}
                caption={`${cameraName(countedCamera)} · counted · ${image.fileName}`}
                widthPx={image.widthPx}
                heightPx={image.heightPx}
                marks={countedMarks(detections)}
              />
            ),
          },
          ...(twin
            ? [
                {
                  id: 'twin',
                  label: cameraName(twin.camera),
                  content: (
                    <AnnotatedImage
                      src={wfpApi.surveyImageUrl(twin.id)}
                      alt={`Reference ${twinName} photo ${twin.fileName}`}
                      caption={`${cameraName(twin.camera)} · reference, not counted · ${twin.fileName}`}
                      widthPx={twin.widthPx}
                      heightPx={twin.heightPx}
                      marks={twinMarks(check)}
                    />
                  ),
                },
              ]
            : []),
        ]}
      />
      {twinError && <Alert tone="warning">Couldn't load the marks on the {twinName} photo.</Alert>}

      <Modal
        open={showInfo}
        onClose={() => setShowInfo(false)}
        title="Photo findings & details"
        footer={
          <Button variant="outline" className="min-h-11" onClick={() => setShowInfo(false)}>
            Close
          </Button>
        }
      >
        <div className="min-w-0 space-y-3 text-sm text-gray-700 dark:text-gray-300">
          <dl className="grid grid-cols-[1fr_auto] gap-x-3 gap-y-1">
            <dt>Found by AI, accepted</dt>
            <dd className="text-right font-medium">{byAi}</dd>
            <dt>Added by ranger (AI missed)</dt>
            <dd className="text-right font-medium">{byRanger}</dd>
            <dt>AI suggestions rejected</dt>
            <dd className="text-right font-medium">{rejected}</dd>
            {pending > 0 && (
              <>
                <dt>AI suggestions still to check</dt>
                <dd className="text-right font-medium">{pending}</dd>
              </>
            )}
            {twin && (
              <>
                <dt className="col-span-2 mt-2 text-xs font-semibold uppercase tracking-wide text-gray-500 dark:text-gray-400">
                  On the {twinName} photo
                </dt>
                <dt>Seen there too</dt>
                <dd className="text-right font-medium">{seenOnBoth}</dd>
                <dt>Not visible there</dt>
                <dd className="text-right font-medium">{notOnTwin}</dd>
                <dt>Not checked there</dt>
                <dd className="text-right font-medium">{Math.max(0, accepted.length - seenOnBoth - notOnTwin)}</dd>
                {onlyOnTwin > 0 && (
                  <>
                    <dt>Seen only there (not counted)</dt>
                    <dd className="text-right font-medium">{onlyOnTwin}</dd>
                  </>
                )}
              </>
            )}
          </dl>
          {accepted.length > 0 && (
            <ol
              className="space-y-1 border-t border-gray-200 pt-2 text-xs dark:border-gray-700"
              aria-label="Each confirmed pig"
            >
              {detections.map((d, i) =>
                d.status !== 'accepted' ? null : (
                  <li key={d.id} className="flex gap-2">
                    <span
                      className="inline-flex size-5 shrink-0 items-center justify-center rounded font-semibold text-gray-950"
                      style={{ background: boxStyle(d).color }}
                    >
                      {i + 1}
                    </span>
                    <span>
                      {d.source === 'ranger'
                        ? 'Added by ranger'
                        : `AI${d.confidence != null ? ` ${percent(d.confidence)}` : ''}, accepted`}
                      {twin && ` · ${pigOnTwin(markOf.get(d.id)?.state, twinName)}`}
                    </span>
                  </li>
                ),
              )}
            </ol>
          )}
          <dl className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1 border-t border-gray-200 pt-2 text-xs dark:border-gray-700">
            <dt className="text-gray-500 dark:text-gray-400">Time (NT)</dt>
            <dd>{image.takenAt ? ntTime(image.takenAt) : 'Unknown'}</dd>
            <dt className="text-gray-500 dark:text-gray-400">Location</dt>
            <dd>
              {image.latitude != null && image.longitude != null
                ? `${image.latitude.toFixed(5)}, ${image.longitude.toFixed(5)}`
                : 'No GPS'}
            </dd>
            {image.reviewedBy && (
              <>
                <dt className="text-gray-500 dark:text-gray-400">Checked by</dt>
                <dd className="break-words">
                  {image.reviewedBy}
                  {image.reviewedAt
                    ? ` · ${new Date(image.reviewedAt).toLocaleDateString('en-AU', { timeZone: 'Australia/Darwin' })}`
                    : ''}
                </dd>
              </>
            )}
          </dl>
          <p className="text-xs text-gray-500 dark:text-gray-400">
            Green: AI, accepted. Blue: added by a ranger. Numbers match the review tool.
            {twin ? ` Purple: the same pig on the ${twinName} photo. Amber dashed: seen only there.` : ''}
          </p>
          <ButtonLink
            to={wfpPath(`/surveys/${surveyId}/review?photo=${encodeURIComponent(image.id)}`)}
            variant="outline"
            block
            className="min-h-11"
            icon={<ExternalLink size={16} aria-hidden="true" />}
          >
            Open in review
          </ButtonLink>
        </div>
      </Modal>
    </div>
  );
}
