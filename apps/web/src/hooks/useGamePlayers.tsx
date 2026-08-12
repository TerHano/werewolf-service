import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { useApiQuery } from "./useApiQuery";

export const gamePlayersQueryKey = "game-players";

/**
 * Everyone dealt into the game, keyed by player role id so it lines up with the target ids in
 * a role action. Carries no roles.
 */
export const useGamePlayers = (roomId: string) => {
  return useApiQuery<GamePlayerDto[]>({
    queryKey: [gamePlayersQueryKey, roomId],
    query: {
      endpoint: `game/${roomId}/players`,
    },
  });
};
