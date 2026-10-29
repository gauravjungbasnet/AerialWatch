import { useCallback, useRef, useState } from 'react';
import { ApiError } from '../../../../../shared/api/client';
import { wfpApi } from '../../../api/client';
import type { UploadFileResult } from '../../../api/types';
import { useRefreshSurveyPhotos } from '../hooks';
import { batches, checkFiles } from './uploadRules';

export interface UploadProgress {
  running: boolean;
  total: number; // files being sent
  done: number; // files answered by the API (any status)
  /** Every file sent this session (all uploads since the page opened, newest batch last). */
  results: UploadFileResult[];
  error?: string;
  cancelled?: boolean;
}

const idle: UploadProgress = { running: false, total: 0, done: 0, results: [] };

/**
 * Sends photos in batches of 20, one batch at a time, so a large folder shows steady progress and one failed batch
 * doesn't lose the rest. Files skipped locally (wrong type, too big) are reported as rejected without being sent.
 */
export function useUploadPhotos(surveyId: string) {
  const [progress, setProgress] = useState<UploadProgress>(idle);
  const abort = useRef<AbortController | null>(null);
  const refresh = useRefreshSurveyPhotos(surveyId);

  const start = useCallback(
    async (picked: File[]) => {
      const { accepted, skipped } = checkFiles(picked);
      const local: UploadFileResult[] = skipped.map((s) => ({
        fileName: s.fileName,
        status: 'rejected',
        message: s.reason,
      }));
      const controller = new AbortController();
      abort.current = controller;
      // Keep earlier uploads in the session list; progress counts only this upload.
      setProgress((p) => ({ running: true, total: accepted.length, done: 0, results: [...p.results, ...local] }));

      let error: string | undefined;
      for (const batch of batches(accepted)) {
        if (controller.signal.aborted) break;
        try {
          const response = await wfpApi.uploadSurveyImages(surveyId, batch, controller.signal);
          setProgress((p) => ({ ...p, done: p.done + batch.length, results: [...p.results, ...response.files] }));
        } catch (e) {
          if (controller.signal.aborted) break;
          error = e instanceof ApiError ? e.message : 'Upload failed. Please try again.';
          break;
        }
        await refresh(); // photos appear in the grid as each batch lands
      }

      const cancelled = controller.signal.aborted;
      abort.current = null;
      setProgress((p) => ({ ...p, running: false, error, cancelled }));
      await refresh();
    },
    [surveyId, refresh],
  );

  const cancel = useCallback(() => abort.current?.abort(), []);
  const clear = useCallback(() => setProgress(idle), []);

  return { progress, start, cancel, clear };
}
