import type { ValidationProblem } from './types';

// Empty base = same origin (Vite dev server proxies /api to the .NET API). Override with VITE_API_BASE if hosted separately.
const API_BASE = import.meta.env.VITE_API_BASE ?? '';

export class ApiError extends Error {
  readonly status: number;
  readonly fieldErrors: Record<string, string[]>;

  constructor(status: number, message: string, fieldErrors: Record<string, string[]> = {}) {
    super(message);
    this.status = status;
    this.fieldErrors = fieldErrors;
  }
}

export async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response;
  try {
    // FormData (file uploads) must set its own multipart Content-Type with the boundary, so leave it to the browser.
    const isForm = typeof FormData !== 'undefined' && init?.body instanceof FormData;
    response = await fetch(`${API_BASE}${path}`, {
      ...init,
      headers: { ...(isForm ? {} : { 'Content-Type': 'application/json' }), ...(init?.headers ?? {}) },
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === 'AbortError') throw error;
    throw new ApiError(0, 'Cannot reach the Aerial Watch API. Is the backend running on port 5080?');
  }

  if (!response.ok) {
    let problem: ValidationProblem | undefined;
    try {
      problem = (await response.json()) as ValidationProblem;
    } catch {
      /* body was not JSON */
    }
    const fieldErrors = problem?.errors ?? {};
    const first = Object.values(fieldErrors)[0]?.[0];
    throw new ApiError(response.status, first ?? problem?.title ?? `Request failed (${response.status})`, fieldErrors);
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

/** Module clients own their prefix; callers supply only module-relative endpoint paths. */
export function createApiClient(prefix: `/api/${string}`) {
  return {
    request<T>(endpoint: `/${string}`, init?: RequestInit): Promise<T> {
      return request<T>(`${prefix}${endpoint}`, init);
    },
    /** Absolute URL of an endpoint, for <img src> and links (e.g. photo files served by the API). */
    url(endpoint: `/${string}`): string {
      return `${API_BASE}${prefix}${endpoint}`;
    },
  };
}
