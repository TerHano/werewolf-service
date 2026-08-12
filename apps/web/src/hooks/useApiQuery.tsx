import { getApi } from "@/util/api";
import { useQuery } from "@tanstack/react-query";

export interface QueryOptions<T> {
  initialData?: T;
  enabled?: boolean;
  staleTime?: number;
  cacheTime?: number;
}

export interface useApiQueryProps<T> {
  queryKey: string[];
  query: {
    endpoint: string;
    params?: URLSearchParams;
  };
  options?: QueryOptions<T>;
}

export const useApiQuery = <T,>({
  queryKey,
  query: { endpoint, params },
  options: { initialData, enabled, staleTime = 5000, cacheTime } = {
    enabled: true,
  },
}: useApiQueryProps<T>) => {
  let queryParams = "";
  if (params) {
    queryParams = "?" + params.toString();
  }
  const url = `${import.meta.env.WEREWOLF_SERVER_URL}/api/${endpoint}${queryParams}`;
  return useQuery({
    initialData: initialData,
    queryKey: queryKey,
    queryFn: () =>
      getApi<T>({
        url,
        method: "GET",
      }),
    staleTime,
    enabled,
    gcTime: cacheTime,
  });
};
