import type { SurveyStatus } from '../api/types';
import { Badge, type Tone } from '@shared/controls';

const MAP: Record<SurveyStatus, { label: string; tone: Tone }> = {
  draft: { label: 'Draft', tone: 'neutral' },
  uploading: { label: 'Uploading', tone: 'info' },
  processing: { label: 'AI running', tone: 'primary' },
  inReview: { label: 'In review', tone: 'warning' },
  reviewed: { label: 'Reviewed', tone: 'success' },
  failed: { label: 'Failed', tone: 'danger' },
};

export function SurveyStatusBadge({ status }: { status: SurveyStatus }) {
  const s = MAP[status];
  return <Badge tone={s.tone}>{s.label}</Badge>;
}
