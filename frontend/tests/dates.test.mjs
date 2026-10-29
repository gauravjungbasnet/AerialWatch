import assert from 'node:assert/strict';
import test from 'node:test';
import { loadTs } from './load-ts.mjs';

const { calendarDate, ntDate, ntDateTime, ntTime } = loadTs('shared/lib/dates.ts');

test('date-only values never move to another day, whatever the browser zone', () => {
  const options = { weekday: 'short', day: 'numeric', month: 'short', year: 'numeric' };
  assert.match(calendarDate('2026-10-20', options), /Tue,? 20 Oct 2026/);
  assert.match(calendarDate('2026-01-01'), /^0?1\/0?1\/2026$/);
});

test('timestamps are shown in NT time (UTC+9:30, no daylight saving)', () => {
  assert.equal(ntTime('2026-10-20T08:51:05Z'), '18:21:05');
  assert.match(ntDate('2026-10-20T20:00:00Z'), /^21\/10\/2026$/); // already the next day in Darwin
  assert.match(ntDateTime('2026-10-20T08:51:05Z'), /^20\/10\/2026,? 18:21:05$/);
});
