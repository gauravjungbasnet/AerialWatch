// Shared enums and notes used by every WFP feature. Mirrors the .NET contracts (backend/AerialWatch.Contracts); enums are camelCase strings.

export type Season = 'wet' | 'dry';
export type CameraType = 'thermal' | 'rgb';
export type TimeWindow = 'earlyMorning' | 'midMorning' | 'midAfternoon' | 'earlyEvening';
export type DetectionModelKind = 'timeSeason' | 'environment';

export interface Assumption {
  level: 'info' | 'warning';
  message: string;
}
