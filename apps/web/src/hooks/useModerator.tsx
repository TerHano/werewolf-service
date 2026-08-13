import { PlayerDto } from "@/dto/PlayerDto";
import { useApiQuery } from "./useApiQuery";

export const moderatorQueryKey = "moderator";

export const useModerator = (roomId: string) => {
  return useApiQuery<PlayerDto | null>({
    queryKey: [moderatorQueryKey, roomId],
    query: {
      endpoint: `room/${roomId}/get-moderator`,
    },
  });
};
