import { mutationOptions, useApiMutation } from "./useApiMutation";

export const useDeleteQueuedAction = (
  options: mutationOptions<void, number> = {}
) => {
  return useApiMutation<void, number>({
    mutation: {
      queryKeysToInvalidate: [["queued-action"], ["all-queued-actions"]],
      endpoint: "game/queued-action/{id}",
      method: "DELETE",
    },
    ...options,
  });
};
