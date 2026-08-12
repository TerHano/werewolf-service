import { useRoomId } from "@/hooks/useRoomId";
import { ModeratorView } from "./ModeratorView/ModeratorView";
import { PlayerView } from "@/components/GameRoom/PlayerView/PlayerView";
import { useIsModerator } from "@/hooks/useIsModerator";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { useQueryClient } from "@tanstack/react-query";
import { querysToInvalidateOnNewGame } from "@/hooks/useStartGame";
import { Skeleton } from "@/components/ui-addons/skeleton";
import { lazy } from "react";
import { useWinCondition } from "@/hooks/useWinCondition";
import { useRoomRoleSettings } from "@/hooks/useRoomRoleSettings";

const WinConditionPage = lazy(
  () => import("@/components/GameRoom/WinConditionPage")
);

const SelfModeratedView = lazy(
  () => import("@/components/GameRoom/SelfModeratedView/SelfModeratedView")
);

export const GameRoom = () => {
  const roomId = useRoomId();

  const {
    data: winCondition,
    refetch,
    isLoading: isWinConditionLoading,
  } = useWinCondition(roomId);

  const { data: isModerator, isLoading: isModeratorLoading } =
    useIsModerator(roomId);
  const { data: roleSettings, isLoading: areSettingsLoading } =
    useRoomRoleSettings(roomId);
  const queryClient = useQueryClient();

  useSocketConnection({
    onWinConditionMet: () => {
      refetch();
    },
    onGameRestart: () => {
      querysToInvalidateOnNewGame.forEach((q) => {
        queryClient.invalidateQueries({ queryKey: q });
      });
    },
  });

  if (isWinConditionLoading || isModeratorLoading || areSettingsLoading) {
    return <Skeleton loading={true} height={100} />;
  }
  if (winCondition) {
    return (
      <WinConditionPage winCondition={winCondition} isModerator={isModerator} />
    );
  }
  // In a self-moderated room there is no moderator screen: the server runs the night and
  // everyone, host included, plays.
  if (roleSettings?.selfModerated) {
    return <SelfModeratedView />;
  }
  if (isModerator) {
    return <ModeratorView />;
  }
  return <PlayerView />;
};
