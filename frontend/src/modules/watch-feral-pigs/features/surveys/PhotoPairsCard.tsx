import { useState, type ReactNode } from 'react';
import { ArrowLeftRight, MapPin } from 'lucide-react';
import type { CameraType, SurveyImage } from '../../api/types';
import { wfpApi } from '../../api/client';
import { Badge, Button, Card } from '@shared/controls';
import { cx } from '@shared/lib/format';
import { useSurveyImages } from './hooks';
import { cameraName } from '../../domain/cameras';
import { summarisePairs, type PhotoPair } from './pairs';

const PAGE = 8;

/**
 * Thermal + colour twins (D-039): how the uploaded photos were paired, and each pair side by side. Only shown when the
 * survey has photos from both cameras. The survey's own camera is counted; the other camera's photos are reference only.
 */
export function PhotoPairsCard({ surveyId, camera }: { surveyId: string; camera: CameraType }) {
  const images = useSurveyImages(surveyId);
  const [shown, setShown] = useState(PAGE);
  const list = images.data ?? [];
  const sum = summarisePairs(list);
  if (sum.reference === 0 && sum.paired === 0) return null;

  const other: CameraType = camera === 'thermal' ? 'rgb' : 'thermal';
  const rows = sum.pairs
    .filter((p) => p.counted && p.reference)
    .concat(sum.pairs.filter((p) => !(p.counted && p.reference)));

  return (
    <Card
      title={
        <span className="inline-flex items-center gap-2">
          <ArrowLeftRight className="size-4" aria-hidden="true" /> {cameraName(camera)} +{' '}
          {cameraName(other).toLowerCase()} pairs
        </span>
      }
      description={`${cameraName(camera)} photos are counted. ${cameraName(other)} photos with the same name are kept as reference and shown beside them in the review.`}
    >
      <dl className="grid grid-cols-2 gap-3 text-sm sm:grid-cols-5">
        <Stat label="Pairs" value={sum.paired} />
        <Stat label={`${cameraName(camera)} only`} value={sum.countedOnly} />
        <Stat label={`${cameraName(other)} only`} value={sum.referenceOnly} />
        <Stat label="Counted photos" value={sum.counted} />
        <Stat label="GPS from twin" value={sum.gpsFromTwin} icon={<MapPin className="size-3.5" aria-hidden="true" />} />
      </dl>

      <ul className="mt-5 space-y-3">
        {rows.slice(0, shown).map((p) => (
          <PairRow key={p.key} pair={p} camera={camera} other={other} />
        ))}
      </ul>
      {rows.length > shown && (
        <Button className="mt-4" variant="outline" size="sm" onClick={() => setShown((n) => n + PAGE * 3)}>
          Show more ({rows.length - shown} left)
        </Button>
      )}
    </Card>
  );
}

function Stat({ label, value, icon }: { label: string; value: number; icon?: ReactNode }) {
  return (
    <div className="rounded-lg bg-gray-50 p-3 dark:bg-gray-700">
      <dt className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
        {icon}
        {label}
      </dt>
      <dd className="text-lg font-bold tabular-nums text-gray-900 dark:text-gray-100">{value}</dd>
    </div>
  );
}

function PairRow({ pair, camera, other }: { pair: PhotoPair; camera: CameraType; other: CameraType }) {
  return (
    <li className="grid grid-cols-2 gap-3">
      <Side image={pair.counted} camera={camera} counted />
      <Side image={pair.reference} camera={other} />
    </li>
  );
}

function Side({ image, camera, counted = false }: { image?: SurveyImage; camera: CameraType; counted?: boolean }) {
  if (!image)
    return (
      <div className="grid aspect-[4/3] place-items-center rounded-lg border border-dashed border-gray-300 p-2 text-center text-xs text-gray-500 dark:border-gray-600 dark:text-gray-400">
        No {cameraName(camera).toLowerCase()} twin
      </div>
    );
  return (
    <figure className="min-w-0">
      <a
        href={wfpApi.surveyImageUrl(image.id)}
        target="_blank"
        rel="noreferrer"
        className="block overflow-hidden rounded-lg bg-gray-900 focus:outline-none focus-visible:ring-focus"
        aria-label={`Open ${image.fileName} full size`}
      >
        <img
          src={image.hasThumbnail ? wfpApi.surveyThumbnailUrl(image.id) : wfpApi.surveyImageUrl(image.id)}
          alt={`${cameraName(camera)} photo ${image.fileName}`}
          loading="lazy"
          className="aspect-[4/3] w-full object-contain"
        />
      </a>
      <figcaption className="mt-1 flex min-w-0 items-center gap-1.5 text-xs text-gray-600 dark:text-gray-400">
        <Badge tone={counted ? 'primary' : 'neutral'} className={cx('shrink-0')}>
          {counted ? 'Counted' : 'Reference'}
        </Badge>
        <span className="truncate" title={image.fileName}>
          {image.fileName}
        </span>
      </figcaption>
    </figure>
  );
}
