import { WinCondition } from "@/enum/WinCondition";
import { useApiQuery } from "./useApiQuery";

export const useWinCondition = (roomId: string) => {
  return useApiQuery<WinCondition>({
    queryKey: ["win-condition", roomId],
    query: {
      endpoint: `game/${roomId}/check-win-condition`,
    },
    options: { cacheTime: 0 },
  });
};
