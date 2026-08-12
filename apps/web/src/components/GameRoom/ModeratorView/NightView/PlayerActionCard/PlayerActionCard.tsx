import { Card } from "@chakra-ui/react";
import { ActionType } from "@/enum/ActionType";
import { useMemo } from "react";
import { QueuedActionDto } from "@/dto/QueuedActionDto";
import { Role } from "@/enum/Role";
import { PlayerRoleWithDetails } from "../NightCall";
import { PlayerActionCardHeader } from "./PlayerActionCardHeader";
import { PlayerActionCardBody } from "./PlayerActionCardBody";
import { PlayerActionCardFooter } from "@/components/GameRoom/ModeratorView/NightView/PlayerActionCard/PlayerActionCardFooter";

export const PlayerActionCard = ({
  playerDetails,
  allPlayerDetails,
  allQueuedActions,
}: {
  playerDetails: PlayerRoleWithDetails;
  allPlayerDetails: PlayerRoleWithDetails[];
  allQueuedActions: QueuedActionDto[];
}) => {
  const playerRoleActions = playerDetails.actions;

  const queuedAction = useMemo(() => {
    if (playerDetails.role === Role.WereWolf) {
      return allQueuedActions.find(
        (action) => action.action === ActionType.WerewolfKill
      );
    }
    return allQueuedActions.find(
      (action) => action.playerRoleId === playerDetails.id
    );
  }, [allQueuedActions, playerDetails.id, playerDetails.role]);

  return (
    <Card.Root alignItems="center" width="100%" borderBottomRadius={0}>
      <Card.Header>
        <PlayerActionCardHeader
          roleLabel={playerDetails.roleInfo.label}
          isActionQueued={!!queuedAction}
        />
      </Card.Header>
      <Card.Body>
        <PlayerActionCardBody playerDetails={[playerDetails]} />
      </Card.Body>
      <Card.Footer>
        <PlayerActionCardFooter
          playerRoleId={playerDetails.id}
          actions={playerRoleActions}
          queuedAction={queuedAction}
          allPlayerDetails={allPlayerDetails}
        />
      </Card.Footer>
    </Card.Root>
  );
};

export default PlayerActionCard;
