import type { CameraType } from '../api/types';

/** WFP camera labels shared across features (D-039). */
export const cameraName = (camera: CameraType) => (camera === 'thermal' ? 'Thermal' : 'Colour (RGB)');

// Keep the compact picker labels used by planner and new survey.
export const CAMERA_OPTIONS: { value: CameraType; label: string }[] = [
  { value: 'thermal', label: 'Thermal' },
  { value: 'rgb', label: 'RGB' },
];
