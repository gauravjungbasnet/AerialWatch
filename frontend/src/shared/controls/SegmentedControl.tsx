import { cx } from '../lib/format';

export interface SegmentOption<T extends string> {
  value: T;
  label: string;
}

interface SegmentedControlProps<T extends string> {
  name: string;
  legend: string;
  value: T;
  options: SegmentOption<T>[];
  onChange: (value: T) => void;
  className?: string;
  variant?: 'segmented' | 'tabs';
}

/** Accessible radio group rendered as a joined button bar. */
export function SegmentedControl<T extends string>({
  name,
  legend,
  value,
  options,
  onChange,
  className,
  variant = 'segmented',
}: SegmentedControlProps<T>) {
  const tabs = variant === 'tabs';
  return (
    <fieldset className={cx('text-sm', className)}>
      <legend className={tabs ? 'sr-only' : 'mb-1.5 font-medium text-gray-700 dark:text-gray-300'}>{legend}</legend>
      <div
        className={
          tabs ? 'flex w-full gap-5 overflow-x-auto px-4' : 'flex w-full rounded-lg bg-gray-100 p-1 dark:bg-gray-700'
        }
      >
        {options.map((o) => {
          const checked = value === o.value;
          return (
            <label
              key={o.value}
              className={cx(
                tabs
                  ? 'flex min-h-11 shrink-0 cursor-pointer items-center border-b-2 px-1 text-center font-medium transition-colors has-[:focus-visible]:ring-focus'
                  : 'flex-1 cursor-pointer rounded-md px-3 py-1.5 text-center font-medium transition-colors has-[:focus-visible]:ring-focus',
                tabs
                  ? checked
                    ? 'border-brand-600 text-brand-700 dark:text-brand-300'
                    : 'border-transparent text-gray-600 hover:text-gray-900 dark:text-gray-400 dark:hover:text-gray-100'
                  : checked
                    ? 'bg-brand-600 text-white shadow-sm'
                    : 'text-gray-600 hover:bg-gray-200 hover:text-gray-900 dark:text-gray-300 dark:hover:bg-gray-600',
              )}
            >
              <input
                type="radio"
                name={name}
                value={o.value}
                checked={checked}
                onChange={() => onChange(o.value)}
                className="sr-only"
              />
              {o.label}
            </label>
          );
        })}
      </div>
    </fieldset>
  );
}
