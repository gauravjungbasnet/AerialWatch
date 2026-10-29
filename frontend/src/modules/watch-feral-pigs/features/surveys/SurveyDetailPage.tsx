import { useParams } from 'react-router-dom';
import { Alert, ErrorAlert, SkeletonRows } from '@shared/controls';
import { SurveyHeader } from './SurveyHeader';
import { SurveyStatsBar } from './SurveyStatsBar';
import { TrainingSplitCard } from './TrainingSplitCard';
import { PhotoPairsCard } from './PhotoPairsCard';
import { cameraName } from '../../domain/cameras';
import { useSurvey } from '../../api/queries/surveys';
import { PhotoGrid } from './photos/PhotoGrid';
import { PhotoUploader } from './photos/PhotoUploader';

/**
 * Survey page: full-width conditions, uploads and gallery. Notes and checks open from the header.
 */
export function SurveyDetailPage() {
  const { id = '' } = useParams();
  const survey = useSurvey(id);

  if (survey.isLoading)
    return (
      <div className="my-6">
        <SkeletonRows rows={6} />
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
  const canEditPhotos = s.status === 'draft' || s.status === 'uploading'; // same rule as the API (D-030)
  // After review starts, only the other camera's reference photos can still be added (D-040, same rule as the API).
  const canAddReferences = s.status === 'processing' || s.status === 'inReview' || s.status === 'reviewed';
  const otherCamera = cameraName(s.camera === 'thermal' ? 'rgb' : 'thermal').toLowerCase();

  return (
    <>
      <SurveyHeader survey={s} />

      <div className="min-w-0 space-y-8">
        <SurveyStatsBar survey={s} />
        {canEditPhotos ? (
          <PhotoUploader surveyId={s.id} />
        ) : canAddReferences ? (
          <>
            <Alert tone="info">
              Review has started, so the {cameraName(s.camera).toLowerCase()} photos are locked. You can still add{' '}
              {otherCamera} twins (same file names) to see them side by side in the review; they are never counted.
            </Alert>
            <PhotoUploader surveyId={s.id} />
          </>
        ) : (
          <Alert tone="info">Review has started, so photos can no longer be added or removed.</Alert>
        )}
        <PhotoGrid surveyId={s.id} editable={canEditPhotos} />
        <PhotoPairsCard surveyId={s.id} camera={s.camera} />
        <TrainingSplitCard survey={s} />
      </div>
    </>
  );
}
