import { useCallback, useState } from 'react';
import { useParams } from 'react-router-dom';
import { ArrowLeft, Images, Info, MapPin } from 'lucide-react';
import {
  Alert,
  Badge,
  BottomSheet,
  Button,
  ButtonLink,
  Card,
  Collapse,
  ErrorAlert,
  InfoCard,
  Modal,
  SkeletonRows,
} from '@shared/controls';
import { AssumptionsPanel, CitationBlock } from '../../components';
import { percent } from '@shared/lib/format';
import { wfpPath } from '../../paths';
import { useSurvey } from '../../api/queries/surveys';
import { AiVsRangerCard } from './AiVsRangerCard';
import { TwinCheckSummary } from './TwinCheckSummary';
import { PhotoMap } from './PhotoMap';
import { PhotosWithPigsTable } from './PhotosWithPigsTable';
import { PhotoEvidence } from './PhotoEvidence';
import { useSurveyResults } from './hooks';
import './results.css';

/** Map workspace on large screens; a single readable column on phones and short windows. */
export function ResultsPage() {
  const { id = '' } = useParams();
  const survey = useSurvey(id);
  const results = useSurveyResults(id);
  const [showInfo, setShowInfo] = useState(false);
  const [selectedPhoto, setSelectedPhoto] = useState<string>();
  const [photosOpen, setPhotosOpen] = useState(false);
  const [photoFilter, setPhotoFilter] = useState<string>();
  const closeInfo = useCallback(() => setShowInfo(false), []);
  const selectMapPhoto = useCallback((imageId: string) => {
    setSelectedPhoto(imageId);
    setPhotoFilter(imageId);
    setPhotosOpen(true);
  }, []);
  const closePhotos = useCallback(() => setPhotosOpen(false), []);

  if (survey.isLoading || results.isLoading)
    return (
      <div className="p-6">
        <SkeletonRows rows={8} />
      </div>
    );
  if (survey.isError)
    return (
      <div className="p-6">
        <ErrorAlert error={survey.error} title="Couldn't load this survey" />
      </div>
    );
  if (results.isError)
    return (
      <div className="p-6">
        <ErrorAlert error={results.error} title="Couldn't load the results" />
      </div>
    );
  const s = survey.data;
  const r = results.data;
  if (!s || !r) return null;
  const warnings = [...(r.estimate?.assumptions ?? []), ...r.notes].filter((a) => a.level === 'warning');
  const hasGps = r.photos.some((p) => p.latitude != null && p.longitude != null);

  return (
    <div className={`results-workspace ${hasGps ? 'results-workspace--map' : ''}`}>
      <section className="results-summary relative z-10 min-w-0" aria-label="Survey results">
        <Card bodyClassName="p-4! sm:p-5!" className="shadow-lg">
          <div className="space-y-4">
            <div>
              <ButtonLink
                to={wfpPath(`/surveys/${s.id}`)}
                variant="link"
                className="min-h-11"
                icon={<ArrowLeft size={16} aria-hidden="true" />}
              >
                Back to survey
              </ButtonLink>
              <h1 className="break-words text-lg font-bold text-gray-900 dark:text-gray-100">{s.name}</h1>
              <p className="mt-1 break-words text-xs text-gray-600 dark:text-gray-400">
                {s.siteName} · {s.surveyDate} · {s.startTime} · {s.camera === 'thermal' ? 'Thermal' : 'RGB'}
              </p>
            </div>
            <div className="flex flex-wrap items-center justify-between gap-2">
              <Badge tone={r.provisional ? 'warning' : 'success'}>
                {r.provisional ? 'Review incomplete' : 'Review complete'}
              </Badge>
              <span className="text-xs text-gray-600 dark:text-gray-400">
                {r.counts.reviewedPhotos} / {r.counts.photos} checked
              </span>
            </div>
            <div aria-live="polite" className="space-y-3">
              {r.estimate ? (
                <InfoCard
                  highlight
                  title={r.provisional ? 'Provisional estimate' : 'Estimated pigs present'}
                  value={`≈ ${r.estimate.estimateRounded} pigs`}
                  sub={`Likely range ${r.estimate.low}–${r.estimate.high}${r.estimate.densityPerKm2 != null ? ` · ${r.estimate.densityPerKm2} per km²` : ''}`}
                />
              ) : (
                <Alert tone="warning" title="Estimate unavailable">
                  {r.estimateUnavailableReason}
                </Alert>
              )}
              <div className="grid grid-cols-2 gap-2">
                <InfoCard title="Confirmed sightings" value={r.counts.confirmed} />
                <InfoCard
                  title="Chance a pig is visible"
                  value={r.estimate ? percent(r.estimate.detectionProbability) : '—'}
                />
              </div>
            </div>
            {r.estimate && (
              <p className="text-xs leading-relaxed text-gray-600 dark:text-gray-400">
                Assumes separate sightings and similar study conditions. Range excludes uncertainty in model
                coefficients.
              </p>
            )}
            {warnings.some((w) => /coefficient|edited|custom/i.test(w.message)) && (
              <Alert tone="warning">Edited model parameters in use. See information.</Alert>
            )}
            <Button
              variant="outline"
              block
              className="min-h-11"
              icon={<Info size={16} aria-hidden="true" />}
              onClick={() => {
                setPhotosOpen(false);
                setShowInfo(true);
              }}
            >
              Information
              {warnings.length > 0
                ? ` · ${warnings.length} ${warnings.length === 1 ? 'caution' : 'cautions'}`
                : ' & method'}
            </Button>
            <ButtonLink
              to={wfpPath(`/surveys/${s.id}/review`)}
              block
              className="min-h-11"
              variant={r.status === 'reviewed' ? 'outline' : 'primary'}
            >
              {r.status === 'reviewed' ? 'View review' : 'Continue review'}
            </ButtonLink>
            <CitationBlock compact />
          </div>
        </Card>
      </section>

      <section className="results-map min-w-0" aria-label="Photo locations">
        {hasGps ? (
          <PhotoMap photos={r.photos} selectedPhoto={selectedPhoto} onSelectPhoto={selectMapPhoto} immersive />
        ) : (
          <Card title="No photo locations" className="h-full">
            <p className="flex items-center gap-2 text-sm text-gray-600 dark:text-gray-400">
              <MapPin size={18} aria-hidden="true" />
              These photos have no GPS. Your results and review are still available.
            </p>
          </Card>
        )}
      </section>

      <div className="results-photo-trigger relative z-10">
        <Button
          className="min-h-11 shadow-lg"
          icon={<Images size={18} aria-hidden="true" />}
          aria-expanded={photosOpen}
          onClick={() => {
            setPhotoFilter(undefined);
            setPhotosOpen(true);
          }}
        >
          Photos with pigs · {r.counts.photosWithPigs}
        </Button>
      </div>
      {photosOpen && (
        <div className={`results-photos results-sheet z-20 min-w-0 ${photoFilter ? 'results-sheet--image' : ''}`}>
          <BottomSheet
            open={photosOpen}
            title={
              photoFilter
                ? (r.photos.find((p) => p.imageId === photoFilter)?.fileName ?? 'Selected photo')
                : 'Photos with pigs'
            }
            onClose={closePhotos}
            className={photoFilter ? 'max-h-[80dvh]!' : undefined}
            showHandle={Boolean(photoFilter)}
            titleIcon={
              photoFilter ? <Images className="size-4 shrink-0 text-gray-400" aria-hidden="true" /> : undefined
            }
            actions={
              photoFilter && (
                <Button variant="outline" className="min-h-11" onClick={() => setPhotoFilter(undefined)}>
                  Show all
                </Button>
              )
            }
          >
            {photoFilter ? (
              <PhotoEvidence key={photoFilter} surveyId={s.id} imageId={photoFilter} camera={s.camera} />
            ) : (
              <PhotosWithPigsTable
                photos={r.photos}
                onLocate={selectMapPhoto}
                onShow={selectMapPhoto}
                selectedPhoto={selectedPhoto}
                bare
              />
            )}
          </BottomSheet>
        </div>
      )}

      <Modal
        open={showInfo}
        onClose={closeInfo}
        title="Survey information"
        footer={
          <Button variant="outline" className="min-h-11" onClick={closeInfo}>
            Close
          </Button>
        }
      >
        <div className="space-y-5">
          {r.provisional && (
            <Alert tone="warning" title="Review incomplete">
              {r.provisionalReason ?? 'Finish checking the photos before relying on this estimate.'}
            </Alert>
          )}
          {r.estimate && (
            <>
              <AssumptionsPanel assumptions={r.estimate.assumptions} title="Study assumptions" defaultOpen />
              <Collapse title="Calculation & model" className="[&>summary]:min-h-11">
                <p>{r.estimate.method}</p>
                <p className="mt-2 text-xs">Model {r.estimate.modelVersion}</p>
              </Collapse>
            </>
          )}
          {r.notes.length > 0 && <AssumptionsPanel assumptions={r.notes} title="Survey notes" defaultOpen />}
          <Collapse title="AI review details" className="[&>summary]:min-h-11">
            <AiVsRangerCard counts={r.counts} />
          </Collapse>
          <TwinCheckSummary surveyId={s.id} />
          <p className="text-xs">
            Map dots are camera locations. Connecting lines show photo order, not a recorded flight path.
          </p>
          <CitationBlock compact />
          <p className="text-xs">Student project. Not affiliated with or endorsed by CDU or NACAS.</p>
        </div>
      </Modal>
    </div>
  );
}
