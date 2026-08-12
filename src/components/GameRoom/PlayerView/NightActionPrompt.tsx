import { Badge, Card, Stack, Text } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { useRoomId } from "@/hooks/useRoomId";
import { MyRoleDto } from "@/dto/MyRoleDto";
import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { RoleActionDto } from "@/dto/RoleActionDto";
import { ActionType } from "@/enum/ActionType";
import { InvestigationType } from "@/enum/InvestigationType";
import { usePlayerRoleActions } from "@/hooks/usePlayerRoleActions";
import { useQueuedAction } from "@/hooks/useQueuedAction";
import { useCreateUpdateQueuedAction } from "@/hooks/useCreateUpdateQueuedAction";
import { useDeleteQueuedAction } from "@/hooks/useDeleteQueuedAction";
import { useInvestigatePlayer } from "@/hooks/useInvestigatePlayer";
import { useRoleActionHelper } from "@/hooks/useRoleActionHelper";
import { PlayerList } from "@/components/GameRoom/ModeratorView/NightView/ActionModals/PlayerList";
import { InvestigatedPlayerDto } from "@/dto/InvestigatedPlayerDto";

interface NightActionPromptProps {
  myRole: MyRoleDto;
  players: GamePlayerDto[];
}

/**
 * What a player sees when the step their role acts in is running: their available actions and
 * the targets the server says are legal for each.
 *
 * The list is not computed here. `role-actions` already answers "what may this player do right
 * now", including the reason an ability is unavailable, and the server re-checks the same rules
 * when the action is submitted — so this renders what it is told rather than deciding anything.
 */
export const NightActionPrompt = ({ myRole, players }: NightActionPromptProps) => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const playerRoleId = myRole.playerRoleId.toString();

  const { data: actions } = usePlayerRoleActions(roomId, playerRoleId);
  const { data: queuedAction, refetch: refetchQueuedAction } = useQueuedAction(
    roomId,
    playerRoleId
  );

  const [selectedPlayer, setSelectedPlayer] = useState<number | undefined>();
  const [selectedAction, setSelectedAction] = useState<ActionType | undefined>();
  const [investigation, setInvestigation] = useState<{
    player: InvestigatedPlayerDto;
    isWerewolf: boolean;
  } | null>(null);

  const { getActionButtonProps } = useRoleActionHelper();

  const { mutate: submitAction, isPending: isSubmitting } =
    useCreateUpdateQueuedAction({
      onSuccess: async () => {
        await refetchQueuedAction();
      },
    });

  const { mutate: withdrawAction, isPending: isWithdrawing } =
    useDeleteQueuedAction({
      onSuccess: async () => {
        setSelectedPlayer(undefined);
        await refetchQueuedAction();
      },
    });

  const { mutate: investigatePlayer, isPending: isInvestigating } =
    useInvestigatePlayer({
      onSuccess: async (result) => {
        setInvestigation({
          player: result.playerRole,
          isWerewolf: result.isInvestigationSuccessful,
        });
      },
    });

  const nameFor = (playerRoleIdToName: number) =>
    players.find((player) => player.id === playerRoleIdToName)?.nickname ?? "";

  // The Detective is answered on the spot rather than queued, so their screen shows a verdict
  // instead of a pending choice.
  if (investigation) {
    return (
      <Card.Root className="animate-fade-in-from-bottom">
        <Card.Body>
          <Stack align="center" gap={4}>
            <Text textStyle="accent" fontSize="xl">
              {investigation.player.nickname}
            </Text>
            <Badge
              colorPalette={investigation.isWerewolf ? "red" : "green"}
              size="lg"
            >
              <Text textStyle="accent" fontSize="lg">
                {investigation.isWerewolf
                  ? t("game.night.investigation.isWerewolf")
                  : t("game.night.investigation.isNotWerewolf")}
              </Text>
            </Badge>
            <Text color="dimmed" textAlign="center">
              {t("game.night.investigation.keepItToYourself")}
            </Text>
          </Stack>
        </Card.Body>
      </Card.Root>
    );
  }

  const isActionQueued = queuedAction != null;

  return (
    <Card.Root className="animate-fade-in-from-bottom">
      <Card.Body>
        <Stack gap={5}>
          {isActionQueued ? (
            <Stack align="center" gap={3}>
              <Text textStyle="accent" fontSize="lg">
                {t("game.night.chosen", {
                  player: nameFor(queuedAction.affectedPlayerRoleId),
                })}
              </Text>
              <Text color="dimmed" fontSize="sm" textAlign="center">
                {t("game.night.canChangeUntilStepEnds")}
              </Text>
              <Button
                size="sm"
                variant="subtle"
                colorPalette="red"
                loading={isWithdrawing}
                onClick={() => withdrawAction(queuedAction.id)}
              >
                {t("game.night.undo")}
              </Button>
            </Stack>
          ) : (
            (actions ?? []).map((action: RoleActionDto) => {
              const buttonProps = getActionButtonProps(action.type);
              const targets = players.filter((player) =>
                action.validPlayerIds.includes(player.id)
              );
              const isChosenAction = selectedAction === action.type;

              return (
                <Stack key={action.type} gap={3}>
                  <Text textStyle="accent" fontSize="lg">
                    {buttonProps.label}
                  </Text>

                  {!action.enabled ? (
                    <Text color="dimmed" fontSize="sm">
                      {action.disabledReason ??
                        t("game.night.actionUnavailable")}
                    </Text>
                  ) : (
                    <>
                      <PlayerList
                        players={targets}
                        selectedPlayer={isChosenAction ? selectedPlayer : undefined}
                        onPlayerSelect={(id) => {
                          setSelectedAction(action.type);
                          setSelectedPlayer(id);
                        }}
                      />
                      <Button
                        colorPalette={buttonProps.color}
                        disabled={!isChosenAction || selectedPlayer === undefined}
                        loading={isSubmitting || isInvestigating}
                        onClick={() => {
                          if (selectedPlayer === undefined) return;
                          if (action.type === ActionType.Investigate) {
                            investigatePlayer({
                              roomId,
                              playerRoleId: selectedPlayer,
                              investigationType: InvestigationType.Werewolf,
                            });
                            return;
                          }
                          submitAction({
                            roomId,
                            playerRoleId: myRole.playerRoleId,
                            action: action.type,
                            affectedPlayerRoleId: selectedPlayer,
                          });
                        }}
                      >
                        {buttonProps.icon}
                        {buttonProps.label}
                      </Button>
                    </>
                  )}
                </Stack>
              );
            })
          )}
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};

export default NightActionPrompt;
