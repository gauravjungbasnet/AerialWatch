import type { CameraType, CoefficientEntry, DetectionModelKind, ModelCoefficient } from '../../api/types';

/** One table on the admin page: the coefficients of one model for one camera. */
export interface CoefficientGroup {
  key: string;
  title: string;
  /** Shown under the title, e.g. a term the published model doesn't have. */
  note?: string;
  coefficients: ModelCoefficient[];
}

const MODEL_TITLES: Record<DetectionModelKind, string> = {
  timeSeason: 'Time and season',
  environment: 'Temperature and ground cover',
};

const CAMERA_TITLES: Record<CameraType, string> = {
  thermal: 'thermal camera',
  rgb: 'RGB camera',
};

/**
 * Groups the coefficients from the API (one per row in wfp.CoefficientTypes) by model and camera, in SortOrder.
 * A coefficient type added to the database shows up here without code changes.
 */
export function groupCoefficients(coefficients: ModelCoefficient[]): CoefficientGroup[] {
  const groups = new Map<string, CoefficientGroup>();
  for (const c of [...coefficients].sort((a, b) => a.sortOrder - b.sortOrder)) {
    const key = `${c.model}.${c.camera}`;
    let group = groups.get(key);
    if (!group) {
      group = {
        key,
        title: `${MODEL_TITLES[c.model] ?? c.model} · ${CAMERA_TITLES[c.camera] ?? c.camera}`,
        coefficients: [],
      };
      groups.set(key, group);
    }
    group.coefficients.push(c);
  }

  for (const group of groups.values()) {
    const first = group.coefficients[0];
    if (first.model === 'environment' && !group.coefficients.some((c) => c.term === 'ambientTemperatureC'))
      group.note = 'The published model for this camera has no air-temperature term.';
  }
  return [...groups.values()];
}

/** Editable strings keyed by coefficient code. */
export function toDraft(coefficients: ModelCoefficient[]): Record<string, string> {
  return Object.fromEntries(coefficients.map((c) => [c.code, String(c.current)]));
}

/** Parse the draft back to values. Returns field errors (keyed by code) for anything that isn't a number. */
export function fromDraft(
  draft: Record<string, string>,
  coefficients: ModelCoefficient[],
): { values?: CoefficientEntry[]; errors: Record<string, string> } {
  const errors: Record<string, string> = {};
  const values: CoefficientEntry[] = [];
  for (const c of coefficients) {
    const raw = draft[c.code]?.trim() ?? '';
    const n = Number(raw);
    if (raw === '' || !Number.isFinite(n)) errors[c.code] = 'Enter a number';
    else values.push({ code: c.code, value: n });
  }
  return Object.keys(errors).length ? { errors } : { values, errors };
}
