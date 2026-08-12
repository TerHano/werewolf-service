import { PlayerDto } from "@/dto/PlayerDto";
import { QueryOptions, useApiQuery } from "./useApiQuery";

export const useCurrentPlayer = (
  roomId: string,
  options?: QueryOptions<PlayerDto>
) => {
  return useApiQuery<PlayerDto>({
    queryKey: ["current-player", roomId],
    query: {
      endpoint: `player/${roomId}/player`,
    },
    options,
  });
};
