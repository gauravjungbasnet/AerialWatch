import type { CameraType } from '../../api/types';
import { Card, FormField, Input, SegmentedControl, Select } from '@shared/controls';
import { seasonFromDate } from '../../domain/seasons';
import { CAMERA_OPTIONS, seasonOptions, type SeasonChoice } from './constants';

interface Props {
  date: string;
  onDateChange: (v: string) => void;
  seasonChoice: SeasonChoice;
  onSeasonChange: (v: SeasonChoice) => void;
  camera: CameraType;
  onCameraChange: (v: CameraType) => void;
}

/** Date, season and camera in one row: every result on the page depends on these. */
export function ConditionsCard({ date, onDateChange, seasonChoice, onSeasonChange, camera, onCameraChange }: Props) {
  return (
    <Card id="conditions">
      <h3 className="sr-only">Survey conditions</h3>
      <div className="grid grid-cols-1 items-end gap-4 md:grid-cols-3">
        <FormField label="Survey date" htmlFor="survey-date">
          <Input id="survey-date" type="date" value={date} onChange={(e) => onDateChange(e.target.value)} required />
        </FormField>
        <FormField label="Season" htmlFor="season">
          <Select
            id="season"
            value={seasonChoice}
            options={seasonOptions(seasonFromDate(date))}
            onValueChange={onSeasonChange}
          />
        </FormField>
        <SegmentedControl
          name="camera"
          legend="Camera"
          value={camera}
          options={CAMERA_OPTIONS}
          onChange={onCameraChange}
        />
      </div>
    </Card>
  );
}
