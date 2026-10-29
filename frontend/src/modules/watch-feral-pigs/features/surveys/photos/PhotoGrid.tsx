import { useState } from 'react';
import { Clock, ImageOff, MapPin, Trash2 } from 'lucide-react';
import { Button, EmptyState, ErrorAlert, SkeletonRows } from '../../../../../shared/controls';
import { fileSize, ntTime } from '../../../../../shared/lib/format';
import { wfpApi } from '../../../api/client';
import type { SurveyImage } from '../../../api/types';
import { useDeleteSurveyImage, useSurveyImages } from '../hooks';

/** Gallery of a survey's photos: thumbnail, capture time and GPS (orange when missing), size, remove while editable. */
export function PhotoGrid({ surveyId, editable }: { surveyId: string; editable: boolean }) {
  const images = useSurveyImages(surveyId);

  if (images.isLoading) return <SkeletonRows rows={3} />;
  if (images.isError) return <ErrorAlert error={images.error} title="Couldn't load the photos" />;
  const list = images.data ?? [];

  const withGps = list.filter((i) => i.latitude != null).length;
  const withTime = list.filter((i) => i.takenAt != null).length;

  return (
    <section aria-label="Photos">
      <h3 className="mb-4 px-1 text-sm font-medium text-gray-700 dark:text-gray-300">
        {list.length} photo{list.length === 1 ? '' : 's'} · {withTime} with capture time · {withGps} with GPS
      </h3>
      {list.length === 0 ? (
        <div className="rounded-xl border border-gray-200 bg-white dark:border-gray-700 dark:bg-gray-800">
          <EmptyState
            title="No photos yet"
            description={editable ? 'Upload the photos from this flight to start.' : undefined}
          />
        </div>
      ) : (
        <ul className="grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-4">
          {list.map((image) => (
            <PhotoCard key={image.id} image={image} surveyId={surveyId} editable={editable} />
          ))}
        </ul>
      )}
    </section>
  );
}

function PhotoCard({ image, surveyId, editable }: { image: SurveyImage; surveyId: string; editable: boolean }) {
  const remove = useDeleteSurveyImage(surveyId);
  const [confirming, setConfirming] = useState(false);
  const [broken, setBroken] = useState(false);
  const missing = 'inline-flex items-center gap-1 font-medium text-orange-600 dark:text-orange-400';
  const present = 'inline-flex items-center gap-1 font-medium text-gray-600 dark:text-gray-300';

  return (
    <li className="flex flex-col overflow-hidden rounded-xl border border-gray-200 bg-white shadow-sm transition-colors hover:border-brand-300 dark:border-gray-700 dark:bg-gray-800">
      <a
        href={wfpApi.surveyImageUrl(image.id)}
        target="_blank"
        rel="noreferrer"
        title={`Open ${image.fileName}`}
        className="flex h-28 items-center justify-center border-b border-gray-100 bg-gray-100 text-gray-300 dark:border-gray-700 dark:bg-gray-900"
      >
        {broken ? (
          <ImageOff className="size-8 opacity-50" aria-label="Preview unavailable" />
        ) : (
          <img
            src={wfpApi.surveyThumbnailUrl(image.id)}
            alt={image.fileName}
            loading="lazy"
            onError={() => setBroken(true)}
            className="h-full w-full object-cover"
          />
        )}
      </a>
      <div className="flex flex-1 flex-col p-3 text-xs">
        <p className="mb-1.5 truncate font-semibold text-gray-800 dark:text-gray-100" title={image.fileName}>
          {image.fileName}
        </p>
        <div className="mb-2 flex flex-wrap gap-2">
          {image.takenAt ? (
            <span className={present}>
              <Clock className="size-3" aria-hidden="true" /> {ntTime(image.takenAt)}
            </span>
          ) : (
            <span className={missing}>
              <Clock className="size-3" aria-hidden="true" /> No time
            </span>
          )}
          {image.latitude != null && image.longitude != null ? (
            <span className={present} title={`${image.latitude.toFixed(5)}, ${image.longitude.toFixed(5)}`}>
              <MapPin className="size-3" aria-hidden="true" /> GPS
            </span>
          ) : (
            <span className={missing}>
              <MapPin className="size-3" aria-hidden="true" /> No GPS
            </span>
          )}
        </div>
        <p className="mb-3 mt-auto font-medium text-gray-400">{fileSize(image.sizeBytes)}</p>

        {editable &&
          (confirming ? (
            <div className="flex gap-2">
              <Button
                variant="danger"
                size="sm"
                className="flex-1"
                loading={remove.isPending}
                onClick={() => remove.mutate(image.id)}
              >
                Remove
              </Button>
              <Button
                variant="outline"
                size="sm"
                className="flex-1"
                onClick={() => setConfirming(false)}
                disabled={remove.isPending}
              >
                Keep
              </Button>
            </div>
          ) : (
            <button
              type="button"
              onClick={() => setConfirming(true)}
              aria-label={`Remove ${image.fileName}`}
              className="flex w-full items-center justify-center gap-1.5 rounded border border-gray-200 py-1.5 font-medium text-gray-600 transition-colors hover:border-red-200 hover:bg-red-50 hover:text-red-600 focus:outline-none focus-visible:ring-focus dark:border-gray-600 dark:text-gray-300 dark:hover:bg-red-900/30"
            >
              <Trash2 className="size-3.5" aria-hidden="true" /> Remove
            </button>
          ))}
        {remove.isError && (
          <div className="mt-2">
            <ErrorAlert error={remove.error} title="Couldn't remove" />
          </div>
        )}
      </div>
    </li>
  );
}
