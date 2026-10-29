import type { TimeWindow } from '../../api/types';

const WINDOW_LABEL: Record<TimeWindow, string> = {
  earlyMorning: 'Early morning',
  midMorning: 'Mid-morning',
  midAfternoon: 'Mid-afternoon',
  earlyEvening: 'Early evening',
};

export const windowLabel = (w?: TimeWindow) => (w ? WINDOW_LABEL[w] : 'Outside study hours');
