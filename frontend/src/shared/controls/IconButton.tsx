import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { cx } from '../lib/format';

export type IconButtonTone = 'neutral' | 'success' | 'danger' | 'quietDanger';

const tones: Record<IconButtonTone, string> = {
  neutral:
    'border border-gray-300 text-gray-600 hover:bg-gray-100 dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700',
  success:
    'border border-green-300 text-green-700 hover:bg-green-50 dark:border-green-700 dark:text-green-300 dark:hover:bg-green-900/30',
  danger:
    'border border-red-300 text-red-700 hover:bg-red-50 dark:border-red-700 dark:text-red-300 dark:hover:bg-red-900/30',
  /** No border, red on hover: a delete action at the end of a table row. */
  quietDanger: 'text-gray-500 hover:bg-gray-100 hover:text-red-600 dark:text-gray-400 dark:hover:bg-gray-700',
};

interface IconButtonProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'children' | 'type'> {
  /** Accessible name, e.g. "Accept box 2". Also the tooltip unless `title` is given. */
  label: string;
  icon: ReactNode;
  tone?: IconButtonTone;
  /** `md` is a 44 px touch target (default); `sm` is for dense tables. */
  size?: 'md' | 'sm';
}

/** A button that shows only an icon (docs/18 step 6). Always named for screen readers and always type="button". */
export function IconButton({ label, icon, tone = 'neutral', size = 'md', title, className, ...rest }: IconButtonProps) {
  return (
    <button
      type="button"
      aria-label={label}
      title={title ?? label}
      className={cx(
        'inline-flex shrink-0 items-center justify-center text-sm focus:outline-none focus-visible:ring-focus disabled:cursor-not-allowed disabled:opacity-40',
        size === 'md' ? 'size-11 rounded-lg' : 'rounded-md p-2',
        tones[tone],
        className,
      )}
      {...rest}
    >
      {icon}
    </button>
  );
}

type PlainButtonProps = Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'type'>;

/** A button with no look of its own (a list row, a thumbnail): type="button" and the focus ring, nothing else. */
export const PlainButton = forwardRef<HTMLButtonElement, PlainButtonProps>(function PlainButton(
  { className, ...rest },
  ref,
) {
  return (
    <button
      ref={ref}
      type="button"
      className={cx('focus:outline-none focus-visible:ring-focus', className)}
      {...rest}
    />
  );
});
