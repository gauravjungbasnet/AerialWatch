import { useQuery } from '@tanstack/react-query';
import { wfpApi } from '../../api/client';
import { queryKeys } from '../../api/queryKeys';

export function useModelInfo() {
  return useQuery({ queryKey: queryKeys.modelInfo, queryFn: wfpApi.modelInfo });
}
