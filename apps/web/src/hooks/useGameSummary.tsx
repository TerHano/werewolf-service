import { QueryOptions, useApiQuery } from "./useApiQuery";
import { NightHistoryDto } from "@/dto/NightHistoryDto";

export const useGameSummary = (
  roomId: string,
  options?: QueryOptions<NightHistoryDto[]>
) => {
  return useApiQuery<NightHistoryDto[]>({
    queryKey: ["game-summary", roomId],
    query: {
      endpoint: `game/${roomId}/summary`,
    },
    options,
  });
};
