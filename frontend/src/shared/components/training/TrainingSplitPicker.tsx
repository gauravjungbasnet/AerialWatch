import { SegmentedControl } from '../../controls';
import { trainingSplitLabel, type TrainingSplit } from '../../types/training';

type PickerValue = TrainingSplit | 'none';

interface TrainingSplitPickerProps {
  value: TrainingSplit | null;
  onChange: (value: TrainingSplit | null) => void;
  name?: string;
  legend?: string;
  className?: string;
}

/** Not used | Train | Validation | Test, for one capture session (a survey, a flight). */
export function TrainingSplitPicker({
  value,
  onChange,
  name = 'training-split',
  legend = 'Use for AI training',
  className,
}: TrainingSplitPickerProps) {
  const options: { value: PickerValue; label: string }[] = [
    { value: 'none', label: 'Not used' },
    { value: 'train', label: trainingSplitLabel.train },
    { value: 'validation', label: trainingSplitLabel.validation },
    { value: 'test', label: trainingSplitLabel.test },
  ];
  return (
    <SegmentedControl<PickerValue>
      name={name}
      legend={legend}
      value={value ?? 'none'}
      options={options}
      onChange={(v) => onChange(v === 'none' ? null : v)}
      className={className}
    />
  );
}
