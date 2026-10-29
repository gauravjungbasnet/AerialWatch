import { useState } from 'react';
import { Pencil, RotateCcw } from 'lucide-react';
import { ApiError } from '@shared/api/client';
import type { ModelParametersResponse } from '../../api/types';
import { Alert, Button, ErrorAlert, Modal, PageHeader, SkeletonRows } from '@shared/controls';
import { ntDateTime } from '@shared/lib/dates';
import { fromDraft, groupCoefficients, toDraft } from './parameterFields';
import { ParameterTable } from './ParameterTable';
import { SavedVersionsTable } from './SavedVersionsTable';
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
            <ParameterTable
              key={group.key}
              group={group}
              draft={draft}
              onDraft={(code, value) => setDraft((d) => d && { ...d, [code]: value })}
              errorFor={errorFor}
            />
          ))}

          <SavedVersionsTable items={history.data} loading={history.isLoading} />

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
  const when = ntDateTime(data.updatedAt);
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
