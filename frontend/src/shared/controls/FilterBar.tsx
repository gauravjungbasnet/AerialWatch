import { useId, useState } from 'react';
import { ChevronDown, ChevronUp, FilterX } from 'lucide-react';
import { cx } from '../lib/format';
import { emptyFilters, hasActiveFilters, type FilterValues } from '../lib/filters';

interface FieldBase {
  /** Key in FilterValues (and the query parameter name for server-side filtering). */
  key: string;
  label: string;
}

export type FilterField =
  | (FieldBase & { type: 'text'; placeholder?: string })
  | (FieldBase & { type: 'date' })
  | (FieldBase & { type: 'select'; options: { value: string; label: string }[]; placeholder?: string });

export interface FilterBarProps {
  fields: FilterField[];
  values: FilterValues;
  /** Called on every change. Filter rows directly, or debounce (useDebouncedValue) and query the API. */
  onChange: (values: FilterValues) => void;
  /** Fields shown before "More"; the rest open below it. Default 5. */
  visibleCount?: number;
  className?: string;
}

const control =
  'block w-full rounded-md border border-gray-300 bg-white px-3 py-2 text-sm text-gray-700 placeholder:text-gray-400 ' +
  'focus:border-brand-400 focus:outline-none focus:ring-focus dark:border-gray-600 dark:bg-gray-700 dark:text-gray-200 dark:placeholder:text-gray-500';

/**
 * Shared filter row for any list: labelled text, date and select inputs, "More" for extra fields and "Clear".
 * It only holds the inputs; the page decides how values filter the data (in the browser or on the server).
 */
export function FilterBar({ fields, values, onChange, visibleCount = 5, className }: FilterBarProps) {
  const id = useId();
  const [showMore, setShowMore] = useState(false);
  const primary = fields.slice(0, visibleCount);
  const extra = fields.slice(visibleCount);
  const active = hasActiveFilters(values);
  const extraActive = extra.some((f) => (values[f.key] ?? '').trim() !== '');

  const set = (key: string, value: string) => onChange({ ...values, [key]: value });

  const renderField = (f: FilterField) => {
    const inputId = `${id}-${f.key}`;
    const value = values[f.key] ?? '';
    return (
      <div key={f.key} className="w-full sm:w-52">
        <label htmlFor={inputId} className="mb-1 block text-sm font-medium text-gray-800 dark:text-gray-300">
          {f.label}
        </label>
        {f.type === 'select' ? (
          <span className="relative block">
            <select
              id={inputId}
              className={cx(control, 'appearance-none pr-9')}
              value={value}
              onChange={(e) => set(f.key, e.target.value)}
            >
              <option value="">{f.placeholder ?? 'All'}</option>
              {f.options.map((o) => (
                <option key={o.value} value={o.value}>
                  {o.label}
                </option>
              ))}
            </select>
            <ChevronDown
              className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-gray-400"
              aria-hidden="true"
            />
          </span>
        ) : (
          <input
            id={inputId}
            type={f.type === 'date' ? 'date' : 'search'}
            className={control}
            value={value}
            placeholder={f.type === 'text' ? (f.placeholder ?? `Filter by ${f.label.toLowerCase()}`) : undefined}
            onChange={(e) => set(f.key, e.target.value)}
          />
        )}
      </div>
    );
  };

  const button =
    'inline-flex h-[38px] items-center gap-2 rounded-md border border-gray-300 bg-white px-3 text-sm font-medium text-gray-700 hover:bg-gray-50 focus:outline-none focus-visible:ring-focus disabled:opacity-50 dark:border-gray-600 dark:bg-gray-800 dark:text-gray-300 dark:hover:bg-gray-700';

  return (
    <div role="search" className={cx('flex flex-col gap-4 p-4', className)}>
      <div className="flex flex-wrap items-end gap-4">
        {primary.map(renderField)}
        <div className="ml-auto flex items-end gap-2">
          {extra.length > 0 && (
            <button type="button" className={button} aria-expanded={showMore} onClick={() => setShowMore((v) => !v)}>
              More{extraActive && !showMore ? ' •' : ''}
              {showMore ? (
                <ChevronUp className="size-4" aria-hidden="true" />
              ) : (
                <ChevronDown className="size-4" aria-hidden="true" />
              )}
            </button>
          )}
          <button
            type="button"
            className={button}
            disabled={!active}
            onClick={() => onChange(emptyFilters(fields.map((f) => f.key)))}
          >
            <FilterX className="size-4" aria-hidden="true" />
            Clear
          </button>
        </div>
      </div>
      {showMore && extra.length > 0 && <div className="flex flex-wrap items-end gap-4">{extra.map(renderField)}</div>}
    </div>
  );
}
