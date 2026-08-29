import { useMemo } from "react";
import { usePlayers } from "./usePlayers";
import { useRoomRoleSettings } from "./useRoomRoleSettings";

export interface LobbySeats {
  /** Players the server will deal a card to. */
  playersToDealTo: number;
  /** Cards in the deck: one per selected role, plus the werewolves. */
  cardsInDeck: number;
  /** How many more players have to join before the deck can be dealt. */
  playersNeeded: number;
  /** Players who will draw a Villager because the deck ran out before they did. */
  extraVillagers: number;
  canStartGame: boolean;
  isLoading: boolean;
}

/**
 * Mirrors GameService.IsEnoughPlayersForGame so the lobby can show the moderator how far off
 * the room is instead of letting them find out from a failed start.
 */
export const useLobbySeats = (roomId: string): LobbySeats => {
  const { data: players, isLoading: isPlayersLoading } = usePlayers(roomId);
  const { data: settings, isLoading: isSettingsLoading } =
    useRoomRoleSettings(roomId);

  const playerCount = players?.length ?? 0;

  return useMemo(() => {
    const isLoading = isPlayersLoading || isSettingsLoading;
    // The players endpoint leaves the moderator out. They are dealt in only when the room
    // moderates itself, which is the seat the server adds or subtracts on its side.
    const playersToDealTo = playerCount + (settings?.selfModerated ? 1 : 0);
    const cardsInDeck =
      (settings?.selectedRoles.length ?? 0) +
      (settings?.numberOfWerewolves ?? 1);
    const playersNeeded = Math.max(cardsInDeck - playersToDealTo, 0);

    return {
      playersToDealTo,
      cardsInDeck,
      playersNeeded,
      extraVillagers: Math.max(playersToDealTo - cardsInDeck, 0),
      // A half-loaded room isn't an unstartable one — the server still has the final say.
      canStartGame: isLoading || playersNeeded === 0,
      isLoading,
    };
  }, [playerCount, settings, isPlayersLoading, isSettingsLoading]);
};
