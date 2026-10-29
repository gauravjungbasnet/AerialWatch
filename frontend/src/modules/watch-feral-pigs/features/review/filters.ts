import type { ReviewImageFilter } from '../../api/types';

/** Review queue filters, in the order shown (same values as the API's ReviewImageFilter). */
export const FILTERS: { value: ReviewImageFilter; label: string }[] = [
  { value: 'all', label: 'All' },
  { value: 'notReviewed', label: 'Not checked' },
  { value: 'pending', label: 'AI to check' },
  { value: 'withPigs', label: 'With pigs' },
  { value: 'noPigs', label: 'No pigs' },
  { value: 'flagged', label: 'Flagged' },
];

export const isReviewFilter = (v: string | null): v is ReviewImageFilter => FILTERS.some((f) => f.value === v);
