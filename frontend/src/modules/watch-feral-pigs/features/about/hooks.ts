import { useQuery } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import { modelKeys } from '../../api/keys';

export function useModelInfo() {
  return useQuery({ queryKey: modelKeys.info, queryFn: wfpApi.modelInfo });
}
