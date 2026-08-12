import { QueuedActionRequestDto } from "@/dto/QueueActionRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";

export const useCreateUpdateQueuedAction = (
  options: mutationOptions<void, QueuedActionRequestDto> = {}
) => {
  return useApiMutation<void, QueuedActionRequestDto>({
    mutation: {
      queryKeysToInvalidate: [["queued-action"], ["all-queued-actions"]],
      endpoint: "game/queued-action",
      method: "POST",
    },
    ...options,
  });
};
