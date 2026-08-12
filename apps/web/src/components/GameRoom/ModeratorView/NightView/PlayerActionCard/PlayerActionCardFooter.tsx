import { PlayerRoleWithDetails } from "../NightCall";
import { ActionButtonList } from "@/components/GameRoom/ModeratorView/NightView/PlayerActionCard/ActionButtonList";
import { RoleActionDto } from "@/dto/RoleActionDto";
import { QueuedActionCard } from "@/components/GameRoom/ModeratorView/NightView/PlayerActionCard/QueuedActionCard";
import { QueuedActionDto } from "@/dto/QueuedActionDto";

export const PlayerActionCardFooter = ({
  playerRoleId,
  actions,
  queuedAction,
  allPlayerDetails,
}: {
  playerRoleId?: number;
  actions: RoleActionDto[];
  queuedAction: QueuedActionDto | undefined;
  allPlayerDetails: PlayerRoleWithDetails[];
}) => {
  return (
    <>
      <ActionButtonList
        isVisible={!queuedAction}
        playerRoleId={playerRoleId}
        actions={actions}
      />
      {queuedAction ? (
        <QueuedActionCard
          queuedAction={queuedAction}
          allPlayersDetails={allPlayerDetails}
        />
      ) : null}
    </>
  );
};
