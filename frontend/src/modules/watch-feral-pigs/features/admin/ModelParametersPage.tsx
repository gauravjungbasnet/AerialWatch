import { useState } from 'react';
import { Pencil, RotateCcw } from 'lucide-react';
import { ApiError } from '@shared/api/client';
import type { ModelParametersHistoryItem, ModelParametersResponse } from '../../api/types';
import {
  Alert,
  Badge,
  Button,
  ErrorAlert,
  Input,
  Modal,
  PageHeader,
  SectionTitle,
  SkeletonRows,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHeadCell,
  TableHeader,
  TableRow,
} from '@shared/controls';
import { cx } from '@shared/lib/format';
import { fromDraft, groupCoefficients, toDraft } from './parameterFields';
import {
  useModelParameters,
  useModelParametersHistory,
  useResetModelParameters,
  useSaveModelParameters,
} from './hooks';

export function ModelParametersPage() {
  const params = useModelParameters();
  const history = useModelParametersHistory();
  const save = useSaveModelParameters();
  const reset = useResetModelParameters();

  const [draft, setDraft] = useState<Record<string, string> | null>(null);
  const [localErrors, setLocalErrors] = useState<Record<string, string>>({});
  const [confirmReset, setConfirmReset] = useState(false);

  const editing = draft !== null;
  const data = params.data;
  const groups = data ? groupCoefficients(data.coefficients) : [];
  const serverErrors = save.error instanceof ApiError ? save.error.fieldErrors : {};
  const errorFor = (id: string) => localErrors[id] ?? serverErrors[id]?.[0];

  const startEdit = () => {
    if (!data) return;
    save.reset();
    setLocalErrors({});
    setDraft(toDraft(data.coefficients));
  };

  const cancel = () => {
    setDraft(null);
    setLocalErrors({});
    save.reset();
  };

  const onSave = () => {
    if (!draft || !data) return;
    const { values, errors } = fromDraft(draft, data.coefficients);
    setLocalErrors(errors);
    if (!values) return;
    save.mutate({ values }, { onSuccess: () => setDraft(null) });
  };

  const onReset = () =>
    reset.mutate(undefined, {
      onSuccess: () => {
        setConfirmReset(false);
        setDraft(null);
      },
    });

  return (
    <>
      <PageHeader
        title="Model parameters"
        description="Coefficients used by the survey planner and estimates."
        actions={
          editing ? (
            <>
              <Button variant="outline" onClick={cancel} disabled={save.isPending}>
                Cancel
              </Button>
              <Button onClick={onSave} loading={save.isPending}>
                Save
              </Button>
            </>
          ) : (
            <>
              <Button
                variant="outline"
                icon={<RotateCcw className="size-4" aria-hidden="true" />}
                onClick={() => setConfirmReset(true)}
                disabled={!data || data.isPublished}
              >
                Reset to published
              </Button>
              <Button icon={<Pencil className="size-4" aria-hidden="true" />} onClick={startEdit} disabled={!data}>
                Edit
              </Button>
            </>
          )
        }
      />

      {params.isLoading && <SkeletonRows rows={8} />}
      {params.isError && <ErrorAlert error={params.error} title="Couldn't load parameters" />}

      {data && (
        <div className="space-y-8">
          <StatusAlert data={data} />
          {save.isError && Object.keys(serverErrors).length === 0 && (
            <ErrorAlert error={save.error} title="Couldn't save" />
          )}

          {groups.map((group) => (
            <section key={group.key} aria-labelledby={`${group.key}-title`}>
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
                                  onChange={(e) => setDraft({ ...draft, [id]: e.target.value })}
                                  invalid={!!error}
                                  aria-describedby={error ? `${id}-error` : undefined}
                                  className="mt-0 text-right"
                                />
                                {error && (
                                  <p
                                    id={`${id}-error`}
                                    className="mt-1 whitespace-normal text-left text-xs text-red-600"
                                  >
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
          ))}

          <HistorySection items={history.data} loading={history.isLoading} />

          <p className="text-xs text-gray-500 dark:text-gray-400">
            Published values: {data.source} Values are logit coefficients; the planner turns them into percentages.
            Every save is stored in the database as a new version with its date and author, so earlier versions are
            kept.
          </p>
        </div>
      )}

      <Modal
        open={confirmReset}
        onClose={() => setConfirmReset(false)}
        title="Reset to published values?"
        footer={
          <>
            <Button variant="outline" onClick={() => setConfirmReset(false)} className="w-full sm:w-auto">
              Cancel
            </Button>
            <Button onClick={onReset} loading={reset.isPending} className="w-full sm:w-auto">
              Reset
            </Button>
          </>
        }
      >
        All coefficients go back to the values published in Hvala et al. (2023).
        {reset.isError && (
          <div className="mt-3">
            <ErrorAlert error={reset.error} title="Couldn't reset" />
          </div>
        )}
      </Modal>
    </>
  );
}

function StatusAlert({ data }: { data: ModelParametersResponse }) {
  const when = new Date(data.updatedAt).toLocaleString();
  return data.isPublished ? (
    <Alert tone="success" title="Using the published values">
      Version {data.version}.
    </Alert>
  ) : (
    <Alert tone="warning" title="Using edited values">
      Version {data.version}, changed {when}
      {data.updatedBy ? ` by ${data.updatedBy}` : ''}. Every result shows a warning until you reset.
    </Alert>
  );
}

function HistorySection({ items, loading }: { items?: ModelParametersHistoryItem[]; loading: boolean }) {
  return (
    <section aria-labelledby="history-title">
      <SectionTitle>
        <span id="history-title">Saved versions</span>
      </SectionTitle>
      {loading && <SkeletonRows rows={3} />}
      {items && (
        <TableContainer>
          <Table>
            <TableHeader>
              <tr>
                <TableHeadCell>Version</TableHeadCell>
                <TableHeadCell>Saved</TableHeadCell>
                <TableHeadCell>By</TableHeadCell>
                <TableHeadCell className="text-right">Values</TableHeadCell>
              </tr>
            </TableHeader>
            <TableBody>
              {items.map((item, i) => (
                <TableRow key={item.id}>
                  <TableCell className="font-semibold text-gray-700 dark:text-gray-200">
                    <span className="inline-flex items-center gap-2">
                      {item.version}
                      {i === 0 && <Badge tone="success">In use</Badge>}
                      {item.isPublished && <Badge>Published</Badge>}
                    </span>
                  </TableCell>
                  <TableCell>{new Date(item.savedAt).toLocaleString()}</TableCell>
                  <TableCell>{item.savedBy ?? '—'}</TableCell>
                  <TableCell className="text-right tabular-nums">{item.values.length}</TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </section>
  );
}
