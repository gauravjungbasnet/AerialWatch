import { wfpPath } from '../../paths';
import { useState, type FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import type { CameraType, GroundCover } from '../../api/types';
import { ApiError } from '@shared/api/client';
import {
  Button,
  ButtonLink,
  Card,
  ErrorAlert,
  FormField,
  Input,
  PageHeader,
  SegmentedControl,
  Select,
  Textarea,
} from '@shared/controls';
import { seasonFromDate, todayIso } from '@shared/lib/format';
import { CAMERA_OPTIONS } from '../../domain/cameras';
import { useCreateSurvey } from './hooks';

const GROUND_COVER_OPTIONS: { value: GroundCover; label: string }[] = [
  { value: 'unknown', label: 'Not sure / mixed' },
  { value: 'open', label: 'Open' },
  { value: 'mixed', label: 'Scattered trees' },
  { value: 'dense', label: 'Dense canopy' },
];

export function NewSurveyPage() {
  const navigate = useNavigate();
  const create = useCreateSurvey();
  const [form, setForm] = useState({
    name: '',
    siteName: '',
    surveyDate: todayIso(),
    startTime: '18:15',
    camera: 'thermal' as CameraType,
    altitudeM: '52',
    areaKm2: '',
    groundCover: 'unknown' as GroundCover,
    notes: '',
  });
  const set = <K extends keyof typeof form>(key: K, value: (typeof form)[K]) =>
    setForm((f) => ({ ...f, [key]: value }));
  const fieldErrors = create.error instanceof ApiError ? create.error.fieldErrors : {};
  const err = (k: string) => fieldErrors[k]?.[0];

  const onSubmit = (e: FormEvent) => {
    e.preventDefault();
    create.mutate(
      {
        name: form.name.trim(),
        siteName: form.siteName.trim(),
        surveyDate: form.surveyDate,
        startTime: form.startTime,
        camera: form.camera,
        altitudeM: form.altitudeM ? Number(form.altitudeM) : undefined,
        areaKm2: form.areaKm2 ? Number(form.areaKm2) : undefined,
        groundCover: form.groundCover,
        notes: form.notes.trim() || undefined,
      },
      { onSuccess: (survey) => navigate(wfpPath(`/surveys/${survey.id}`)) },
    );
  };

  return (
    <>
      <PageHeader title="New survey" />
      <Card className="max-w-4xl">
        <form className="space-y-5" onSubmit={onSubmit} noValidate>
          <div className="grid gap-5 md:grid-cols-2">
            <FormField label="Survey name" htmlFor="name" error={err('name')}>
              <Input
                id="name"
                value={form.name}
                onChange={(e) => set('name', e.target.value)}
                placeholder="North paddock, evening"
                maxLength={80}
                invalid={!!err('name')}
              />
            </FormField>
            <FormField label="Site" htmlFor="site" error={err('siteName')}>
              <Input
                id="site"
                value={form.siteName}
                onChange={(e) => set('siteName', e.target.value)}
                placeholder="Katherine Rural Campus"
                maxLength={120}
                invalid={!!err('siteName')}
              />
            </FormField>
            <FormField
              label="Date"
              htmlFor="date"
              hint={`${seasonFromDate(form.surveyDate) === 'wet' ? 'Wet' : 'Dry'} season`}
              error={err('surveyDate')}
            >
              <Input
                id="date"
                type="date"
                value={form.surveyDate}
                onChange={(e) => set('surveyDate', e.target.value)}
              />
            </FormField>
            <FormField label="Start time" htmlFor="time" error={err('startTime')}>
              <Input
                id="time"
                type="time"
                value={form.startTime}
                onChange={(e) => set('startTime', e.target.value)}
                invalid={!!err('startTime')}
              />
            </FormField>
            <div>
              <SegmentedControl
                name="camera"
                legend="Camera to count"
                value={form.camera}
                options={CAMERA_OPTIONS}
                onChange={(v) => set('camera', v)}
              />
              <p className="mt-1 text-xs text-gray-600 dark:text-gray-400">
                {form.camera === 'thermal'
                  ? 'Pigs are counted on thermal photos. You can also upload the colour photos: they show beside each thermal photo to help you check, but are not counted.'
                  : 'Pigs are counted on colour photos. You can also upload the thermal photos: they show beside each colour photo to help you check, but are not counted.'}
              </p>
            </div>
            <FormField label="Ground cover" htmlFor="cover">
              <Select
                id="cover"
                value={form.groundCover}
                options={GROUND_COVER_OPTIONS}
                onValueChange={(v) => set('groundCover', v)}
              />
            </FormField>
            <FormField label="Altitude (m)" htmlFor="alt" hint="Study flew at about 52 m" error={err('altitudeM')}>
              <Input
                id="alt"
                type="number"
                min={1}
                max={500}
                value={form.altitudeM}
                onChange={(e) => set('altitudeM', e.target.value)}
                invalid={!!err('altitudeM')}
              />
            </FormField>
            <FormField label="Area (km²)" htmlFor="area" error={err('areaKm2')}>
              <Input
                id="area"
                type="number"
                min={0}
                step="0.1"
                placeholder="Optional"
                value={form.areaKm2}
                onChange={(e) => set('areaKm2', e.target.value)}
              />
            </FormField>
          </div>
          <FormField label="Notes" htmlFor="notes">
            <Textarea
              id="notes"
              rows={3}
              value={form.notes}
              onChange={(e) => set('notes', e.target.value)}
              maxLength={1000}
              placeholder="Optional"
            />
          </FormField>

          {create.isError && Object.keys(fieldErrors).length === 0 && (
            <ErrorAlert error={create.error} title="Couldn't create the survey" />
          )}

          <div className="flex justify-end gap-3 border-t border-gray-100 pt-5 dark:border-gray-700">
            <ButtonLink to={wfpPath('/surveys')} variant="outline">
              Cancel
            </ButtonLink>
            <Button type="submit" loading={create.isPending}>
              Create survey
            </Button>
          </div>
        </form>
      </Card>
    </>
  );
}
