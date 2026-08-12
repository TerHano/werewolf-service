import { useApiQuery } from "./useApiQuery";
import { GameState } from "@/enum/GameState";

export const useGameState = (roomId: string) => {
  return useApiQuery<GameState>({
    queryKey: ["game-state", roomId],
    query: {
      endpoint: `room/${roomId}/game-state`,
    },
  });
};
