import {
  Badge,
  Input,
  SectionTitle,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHeadCell,
  TableHeader,
  TableRow,
} from '@shared/controls';
import { cx } from '@shared/lib/format';
import type { CoefficientGroup } from './parameterFields';

interface ParameterTableProps {
  group: CoefficientGroup;
  /** The values being edited (as typed), or null when not editing. */
  draft: Record<string, string> | null;
  onDraft(code: string, value: string): void;
  errorFor(code: string): string | undefined;
}

/** One group of model coefficients: published value, current value, and an input per value while editing. */
export function ParameterTable({ group, draft, onDraft, errorFor }: ParameterTableProps) {
  const editing = draft !== null;
  return (
    <section aria-labelledby={`${group.key}-title`}>
      <SectionTitle>
        <span id={`${group.key}-title`}>{group.title}</span>
      </SectionTitle>
      {group.note && <p className="-mt-2 mb-3 text-xs text-gray-500 dark:text-gray-400">{group.note}</p>}
      <TableContainer>
        <Table>
          <TableHeader>
            <tr>
              <TableHeadCell>Parameter</TableHeadCell>
              <TableHeadCell>Meaning</TableHeadCell>
              <TableHeadCell className="text-right">Published</TableHeadCell>
              <TableHeadCell className="w-56 text-right">Current</TableHeadCell>
            </tr>
          </TableHeader>
          <TableBody>
            {group.coefficients.map((c) => {
              const id = c.code;
              const changed = c.current !== c.published;
              const error = errorFor(id);
              return (
                <TableRow key={id}>
                  <TableCell className="font-semibold text-gray-700 dark:text-gray-200">
                    <label htmlFor={editing ? id : undefined}>{c.label}</label>
                  </TableCell>
                  <TableCell className="text-gray-600 dark:text-gray-400">{c.description}</TableCell>
                  <TableCell className="text-right tabular-nums">{c.published}</TableCell>
                  <TableCell className="text-right tabular-nums">
                    {editing ? (
                      <div className="ml-auto w-40">
                        <Input
                          id={id}
                          inputMode="decimal"
                          value={draft[id] ?? ''}
                          onChange={(e) => onDraft(id, e.target.value)}
                          invalid={!!error}
                          aria-describedby={error ? `${id}-error` : undefined}
                          className="mt-0 text-right"
                        />
                        {error && (
                          <p id={`${id}-error`} className="mt-1 whitespace-normal text-left text-xs text-red-600">
                            {error}
                          </p>
                        )}
                      </div>
                    ) : (
                      <span
                        className={cx(
                          'inline-flex items-center gap-2',
                          changed && 'font-semibold text-brand-600 dark:text-brand-300',
                        )}
                      >
                        {changed && <Badge tone="warning">Edited</Badge>}
                        {c.current}
                      </span>
                    )}
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </TableContainer>
    </section>
  );
}
