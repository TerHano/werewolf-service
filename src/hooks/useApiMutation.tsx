import { getApi } from "@/util/api";
import { useMutation, useQueryClient } from "@tanstack/react-query";

export interface mutationOptions<ReturnType, Body> {
  onSuccess?: (data: ReturnType, variables: Body) => Promise<void>;
  onError?: (error: Error, variables: Body) => Promise<void>;
  skipInvalidatingQueries?: boolean;
}

export interface useApiMutationProps<ReturnType, Body>
  extends mutationOptions<ReturnType, Body> {
  // mutationKey?: string[];
  mutation: {
    endpoint: string;
    method?: "PUT" | "POST" | "DELETE";
    queryKeysToInvalidate?: string[][];
  };
}

export const useApiMutation = <ReturnType, Body>({
  mutation: { endpoint, method = "POST", queryKeysToInvalidate },
  onSuccess,
  onError,
  skipInvalidatingQueries,
}: useApiMutationProps<ReturnType, Body>) => {
  const queryClient = useQueryClient();
  const baseUrl = `${import.meta.env.WEREWOLF_SERVER_URL}/api/${endpoint}`;
  return useMutation<ReturnType, Error, Body>({
    mutationFn: (body: Body) => {
      // The id goes in the path, not the body. Derive the URL from baseUrl on every call —
      // mutating a shared variable here meant a second delete reused the first id.
      const isIdInPathDelete = method === "DELETE" && typeof body === "number";
      const url = isIdInPathDelete
        ? baseUrl.replace("{id}", body.toString())
        : baseUrl;
      return getApi<ReturnType>({
        url,
        method,
        body: isIdInPathDelete ? undefined : JSON.stringify(body),
      });
    },
    onSuccess: (data, variables) => {
      if (!skipInvalidatingQueries) {
        if (queryKeysToInvalidate) {
          queryKeysToInvalidate.forEach((queryKey) => {
            queryClient.invalidateQueries({ queryKey: queryKey });
          });
        }
      }
      if (onSuccess) {
        onSuccess(data, variables);
      }
    },
    onError: onError,
  });
};
