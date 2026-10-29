import {
  forwardRef,
  type InputHTMLAttributes,
  type ReactNode,
  type SelectHTMLAttributes,
  type TextareaHTMLAttributes,
} from 'react';
import { cx } from '../lib/format';

// Windmill form control styling
const control =
  'block w-full mt-1 text-sm rounded-md border bg-white px-3 py-2 text-gray-700 placeholder:text-gray-400 ' +
  'focus:outline-none focus:border-brand-400 focus:ring-focus ' +
  'dark:bg-gray-700 dark:text-gray-300 dark:border-gray-600 dark:placeholder:text-gray-500';
const normal = 'border-gray-300';
const invalidCls = 'border-red-600 focus:border-red-400';

interface FieldProps {
  label: ReactNode;
  htmlFor: string;
  hint?: ReactNode;
  error?: string;
  children: ReactNode;
  className?: string;
}

/** Label + control + helper text (Windmill Label / HelperText). */
export function FormField({ label, htmlFor, hint, error, children, className }: FieldProps) {
  return (
    <div className={cx('block text-sm', className)}>
      <label htmlFor={htmlFor} className="text-gray-700 dark:text-gray-400">
        {label}
      </label>
      {children}
      {(error || hint) && (
        <p
          id={`${htmlFor}-hint`}
          role={error ? 'alert' : undefined}
          className={cx('mt-1 text-xs', error ? 'text-red-600 dark:text-red-400' : 'text-gray-600 dark:text-gray-400')}
        >
          {error ?? hint}
        </p>
      )}
    </div>
  );
}

type InputProps = InputHTMLAttributes<HTMLInputElement> & { invalid?: boolean };

export const Input = forwardRef<HTMLInputElement, InputProps>(function Input({ className, invalid, ...rest }, ref) {
  return (
    <input
      ref={ref}
      className={cx(control, invalid ? invalidCls : normal, className)}
      aria-invalid={invalid || undefined}
      {...rest}
    />
  );
});

type TextareaProps = TextareaHTMLAttributes<HTMLTextAreaElement> & { invalid?: boolean };

export function Textarea({ className, invalid, ...rest }: TextareaProps) {
  return (
    <textarea
      className={cx(control, invalid ? invalidCls : normal, className)}
      aria-invalid={invalid || undefined}
      {...rest}
    />
  );
}

export interface SelectOption<T extends string = string> {
  value: T;
  label: string;
}

type SelectProps<T extends string> = Omit<SelectHTMLAttributes<HTMLSelectElement>, 'onChange' | 'value'> & {
  value: T;
  options: SelectOption<T>[];
  onValueChange: (value: T) => void;
};

/** Typed select bound to a string union. */
export function Select<T extends string>({ value, options, onValueChange, className, ...rest }: SelectProps<T>) {
  return (
    <select
      className={cx(control, normal, 'pr-8', className)}
      value={value}
      onChange={(e) => onValueChange(e.target.value as T)}
      {...rest}
    >
      {options.map((o) => (
        <option key={o.value} value={o.value}>
          {o.label}
        </option>
      ))}
    </select>
  );
}
