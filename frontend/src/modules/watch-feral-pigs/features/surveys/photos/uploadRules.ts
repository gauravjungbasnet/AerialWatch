// Mirrors the API's upload rules (AerialWatch.Application/Wfp/SurveyImages/SurveyImageRules.cs, D-030).
// Checked here too so obviously wrong files are skipped before they are sent.

export const MAX_FILE_BYTES = 50 * 1024 * 1024;
export const FILES_PER_BATCH = 20;
export const ACCEPT = '.jpg,.jpeg,.png,image/jpeg,image/png';

const EXTENSIONS = ['.jpg', '.jpeg', '.png'];

export interface LocalCheck {
  accepted: File[];
  skipped: { fileName: string; reason: string }[];
}

/** Splits picked files into ones worth sending and ones skipped locally (with a reason). */
export function checkFiles(files: File[]): LocalCheck {
  const accepted: File[] = [];
  const skipped: LocalCheck['skipped'] = [];
  for (const file of files) {
    const dot = file.name.lastIndexOf('.');
    const ext = dot >= 0 ? file.name.slice(dot).toLowerCase() : '';
    if (!EXTENSIONS.includes(ext)) skipped.push({ fileName: file.name, reason: 'Not a JPEG or PNG photo.' });
    else if (file.size === 0) skipped.push({ fileName: file.name, reason: 'The file is empty.' });
    else if (file.size > MAX_FILE_BYTES) skipped.push({ fileName: file.name, reason: 'Larger than 50 MB.' });
    else accepted.push(file);
  }
  return { accepted, skipped };
}

export function batches<T>(items: T[], size = FILES_PER_BATCH): T[][] {
  const out: T[][] = [];
  for (let i = 0; i < items.length; i += size) out.push(items.slice(i, i + size));
  return out;
}
