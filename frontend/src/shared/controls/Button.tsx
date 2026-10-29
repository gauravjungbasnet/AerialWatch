import type { ButtonHTMLAttributes, ReactNode } from 'react';
import { Link, type LinkProps } from 'react-router-dom';
import { cx } from '../lib/format';

export type ButtonVariant = 'primary' | 'outline' | 'ghost' | 'link' | 'danger';
export type ButtonSize = 'sm' | 'md' | 'lg';

// Windmill button styles
const base =
  'inline-flex items-center justify-center gap-2 font-medium leading-5 transition-colors duration-150 rounded-lg focus:outline-none focus-visible:ring-focus disabled:opacity-50 disabled:cursor-not-allowed';

const variantClass: Record<ButtonVariant, string> = {
  primary: 'text-white bg-brand-600 border border-transparent hover:bg-brand-700 active:bg-brand-600',
  danger: 'text-white bg-red-600 border border-transparent hover:bg-red-700',
  outline:
    'text-gray-600 border border-gray-300 bg-white hover:border-gray-500 hover:text-gray-700 dark:bg-transparent dark:text-gray-400 dark:border-gray-700 dark:hover:border-gray-500',
  ghost: 'text-gray-600 border border-transparent hover:bg-gray-100 dark:text-gray-400 dark:hover:bg-gray-700',
  link: 'text-brand-600 border border-transparent hover:underline dark:text-brand-300 px-0!',
};

const sizeClass: Record<ButtonSize, string> = {
  sm: 'px-3 py-1 text-sm',
  md: 'px-4 py-2 text-sm min-h-10',
  lg: 'px-5 py-3 text-base',
};

interface CommonProps {
  variant?: ButtonVariant;
  size?: ButtonSize;
  icon?: ReactNode;
  block?: boolean;
}

function buttonClasses({ variant = 'primary', size = 'md', block }: CommonProps, extra?: string) {
  return cx(base, variantClass[variant], sizeClass[size], block && 'w-full', extra);
}

export interface ButtonProps extends CommonProps, ButtonHTMLAttributes<HTMLButtonElement> {
  loading?: boolean;
}

/** Standard button. `loading` shows a spinner and disables it. */
export function Button({
  variant,
  size,
  icon,
  block,
  loading,
  disabled,
  className,
  children,
  type = 'button',
  ...rest
}: ButtonProps) {
  return (
    <button
      type={type}
      className={buttonClasses({ variant, size, block }, className)}
      disabled={disabled || loading}
      {...rest}
    >
      {loading ? (
        <span
          className="size-4 animate-spin rounded-full border-2 border-current border-r-transparent"
          aria-hidden="true"
        />
      ) : (
        icon
      )}
      {children}
    </button>
  );
}

export interface ButtonLinkProps extends CommonProps, LinkProps {}

/** A router link styled as a button. */
export function ButtonLink({ variant, size, icon, block, className, children, ...rest }: ButtonLinkProps) {
  return (
    <Link className={buttonClasses({ variant, size, block }, className)} {...rest}>
      {icon}
      {children}
    </Link>
  );
}
