import type { SurveyImage } from '../../api/types';

// Compatibility export for existing pairing consumers.
export { cameraName } from '../../domain/cameras';

export interface PhotoPair {
  key: string;
  /** Photo from the survey's camera: counted and reviewed. */
  counted?: SurveyImage;
  /** The other camera's photo: reference only. */
  reference?: SurveyImage;
}

export interface PairSummary {
  counted: number;
  reference: number;
  paired: number;
  countedOnly: number;
  referenceOnly: number;
  gpsFromTwin: number;
  pairs: PhotoPair[];
}

/** Groups a survey's photos into twins: counted photo + its reference twin, in flight order. */
export function summarisePairs(images: SurveyImage[]): PairSummary {
  const byId = new Map(images.map((i) => [i.id, i]));
  const seen = new Set<string>();
  const pairs: PhotoPair[] = [];
  for (const img of images) {
    if (seen.has(img.id)) continue;
    const twin = img.twinId ? byId.get(img.twinId) : undefined;
    seen.add(img.id);
    if (twin) seen.add(twin.id);
    const counted = !img.isReference ? img : twin && !twin.isReference ? twin : undefined;
    const reference = img.isReference ? img : twin?.isReference ? twin : undefined;
    pairs.push({ key: img.id, counted, reference });
  }
  return {
    counted: images.filter((i) => !i.isReference).length,
    reference: images.filter((i) => i.isReference).length,
    paired: pairs.filter((p) => p.counted && p.reference).length,
    countedOnly: pairs.filter((p) => p.counted && !p.reference).length,
    referenceOnly: pairs.filter((p) => !p.counted && p.reference).length,
    gpsFromTwin: images.filter((i) => i.locationFromTwin).length,
    pairs,
  };
}
