import { PlayerDto } from "@/dto/PlayerDto";
import { useApiQuery } from "./useApiQuery";

export const usePlayers = (roomId: string) => {
  return useApiQuery<PlayerDto[]>({
    queryKey: ["players", roomId],
    query: {
      endpoint: `room/${roomId}/players`,
    },
  });
};
