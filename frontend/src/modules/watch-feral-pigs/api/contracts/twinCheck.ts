// Thermal and colour twin check (D-040, D-041). Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.
import type { CameraType } from './common';

/** suggested = the app's guess; confirmed = placed or accepted by a ranger; notVisible = can't be seen on the twin. */
export type TwinMarkState = 'suggested' | 'confirmed' | 'notVisible';

export interface TwinMark {
  detectionId: string;
  /** Same number as the box label on the counted photo. */
  number: number;
  state: TwinMarkState;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

/** A pig drawn first on the twin photo (D-041): suggested on the counted photo until confirmed or marked not visible. */
export interface TwinSighting {
  markId: string;
  /** "c1", "c2"… on a colour twin ("t1"… on a thermal twin). */
  label: string;
  state: 'suggestedOnCounted' | 'notVisibleOnCounted';
  twinX: number;
  twinY: number;
  twinWidth: number;
  twinHeight: number;
  /** Suggested position on the counted photo. */
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

export interface TwinCheck {
  imageId: string;
  twinImageId?: string;
  /** Ranger-placed pairs in this survey the suggestions are fitted to. */
  learnedFrom: number;
  marks: TwinMark[];
  sightings: TwinSighting[];
}

export interface SetTwinMarkRequest {
  state: Exclude<TwinMarkState, 'suggested'>;
  x?: number;
  y?: number;
  width?: number;
  height?: number;
}

export interface TwinCheckSummary {
  twinCamera?: CameraType;
  pigsWithTwin: number;
  checked: number;
  visibleOnTwin: number;
  notVisibleOnTwin: number;
  /** Drawn on the other camera's photo and not visible on the counted one (never counted). */
  seenOnlyOnTwin: number;
}
