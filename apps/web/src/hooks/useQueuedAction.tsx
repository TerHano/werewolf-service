import { QueuedActionDto } from "@/dto/QueuedActionDto";
import { useApiQuery } from "./useApiQuery";

export const useQueuedAction = (roomId: string, playerId: string) => {
  return useApiQuery<QueuedActionDto | null>({
    queryKey: ["queued-action", roomId, playerId],
    query: {
      endpoint: `game/${roomId}/${playerId}/queued-action`,
    },
  });
};
