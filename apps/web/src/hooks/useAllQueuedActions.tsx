import { QueuedActionDto } from "@/dto/QueuedActionDto";
import { useApiQuery } from "./useApiQuery";

export const useAllQueuedActionsQueryKey = "all-queued-actions";

export const useAllQueuedActions = (roomId: string) => {
  return useApiQuery<QueuedActionDto[]>({
    queryKey: [useAllQueuedActionsQueryKey, roomId],
    query: {
      endpoint: `game/${roomId}/all-queued-actions`,
    },
  });
};
