import type { ReactNode } from 'react';
import { Button } from './Button';

interface ToggleButtonProps {
  /** Whether this tool or option is on; shown as a filled button and announced with aria-pressed. */
  active: boolean;
  label: string;
  onClick(): void;
  icon?: ReactNode;
  disabled?: boolean;
}

/** A labelled on/off tool button, e.g. Select / Draw in an image workspace. */
export function ToggleButton({ active, label, onClick, icon, disabled }: ToggleButtonProps) {
  return (
    <Button
      variant={active ? 'primary' : 'outline'}
      disabled={disabled}
      onClick={onClick}
      icon={icon}
      aria-pressed={active}
      title={label}
      className="min-h-11 px-3!"
    >
      {label}
    </Button>
  );
}
