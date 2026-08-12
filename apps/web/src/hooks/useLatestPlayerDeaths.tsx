import { PlayerDto } from "@/dto/PlayerDto";
import { useApiQuery } from "./useApiQuery";

export const useLatestPlayerDeaths = (roomId: string) => {
  return useApiQuery<PlayerDto[]>({
    queryKey: ["latest-player-deaths", roomId],
    query: {
      endpoint: `game/${roomId}/latest-deaths`,
    },
  });
};
