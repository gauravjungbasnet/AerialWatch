import { createApiClient } from '@shared/api/client';

/** Transport for every WFP endpoint: /api/wfp, cookies, RFC 7807 errors. */
export const client = createApiClient('/api/wfp');
