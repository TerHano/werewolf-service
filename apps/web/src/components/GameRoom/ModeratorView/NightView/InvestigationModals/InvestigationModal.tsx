import { lazy, useCallback, useMemo, useState } from "react";
import {
  DialogBackdrop,
  DialogRoot,
  DialogBody,
  DialogCloseTrigger,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
  DialogFooter,
} from "@/components/ui/dialog";
import { PlayerList } from "../ActionModals/PlayerList";
import {
  DialogContext,
  StepsContent,
  StepsItem,
  StepsList,
  Text,
} from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { useCreateUpdateQueuedAction } from "@/hooks/useCreateUpdateQueuedAction";
import { Button } from "@/components/ui/button";
import { useRoomId } from "@/hooks/useRoomId";
import { StepsRoot } from "@/components/ui/steps";
import { useAllPlayerRoles } from "@/hooks/useAllPlayerRoles";
import { RoleActionDto } from "@/dto/RoleActionDto";
import { IconSearch } from "@tabler/icons-react";
import { useQueryClient } from "@tanstack/react-query";
import {
  InvestigatePlayerResponse,
  useInvestigatePlayer,
} from "@/hooks/useInvestigatePlayer";
import { InvestigationType } from "@/enum/InvestigationType";

const WerewolfInvestigationResult = lazy(
  () => import("./WerewolfInvestigationResult")
);

export interface InvestigationProps {
  playerRoleId: number;
  action: RoleActionDto;
}

interface InvestigationModalProps {
  title: string;
  resultNode: React.ReactNode;
}

export const InvestigationModal = ({
  playerRoleId,
  action,
}: InvestigationProps) => {
  const roomId = useRoomId();
  const { t } = useTranslation();
  const { data: allPlayers } = useAllPlayerRoles(roomId);
  const [step, setStep] = useState(0);
  const queryClient = useQueryClient();
  const [selectedPlayerRoleId, setSelectedPlayerRoleId] = useState<number>();
  const [investigationResult, setInvestigationResult] =
    useState<InvestigatePlayerResponse | null>(null);

  const { mutateAsync: investigatePlayerAsync } = useInvestigatePlayer();
  const { mutate: createUpdateQueuedAction } = useCreateUpdateQueuedAction({
    onSuccess: async (_, request) => {
      const investigation = await investigatePlayerAsync({
        roomId,
        playerRoleId: request.affectedPlayerRoleId,
        investigationType: InvestigationType.Werewolf,
      });
      setInvestigationResult(investigation);
      setStep(1);
    },
    skipInvalidatingQueries: true,
  });

  const alivePlayers =
    allPlayers?.filter(
      (player) => player.isAlive && player.id !== playerRoleId
    ) ?? [];

  const onSubmit = useCallback(() => {
    if (selectedPlayerRoleId) {
      createUpdateQueuedAction({
        roomId,
        playerRoleId: playerRoleId,
        affectedPlayerRoleId: selectedPlayerRoleId,
        action: action.type,
      });
    }
  }, [
    action.type,
    createUpdateQueuedAction,
    playerRoleId,
    roomId,
    selectedPlayerRoleId,
  ]);

  const onModalExit = useCallback(() => {
    queryClient.invalidateQueries({
      queryKey: ["queued-action"],
    });
    queryClient.invalidateQueries({
      queryKey: ["all-queued-actions"],
    });
    setStep(0);
    setSelectedPlayerRoleId(undefined);
  }, [queryClient]);

  const investigationModalProps = useMemo<InvestigationModalProps>(() => {
    switch (action.type) {
      default:
        return {
          title: t("game.roleAction.investigate.werewolf.title"),
          resultNode: (
            <WerewolfInvestigationResult
              investigationResult={investigationResult}
            />
          ),
        };
    }
  }, [action.type, investigationResult, t]);

  return (
    <DialogRoot placement="center" onExitComplete={onModalExit}>
      <DialogBackdrop />
      <DialogTrigger>
        <Button disabled={!action.enabled} size="sm" colorPalette="blue">
          <IconSearch size={14} />
          {t("Investigate")}
        </Button>
      </DialogTrigger>
      <DialogContent height="24rem">
        <DialogCloseTrigger />
        <DialogHeader>
          <DialogTitle>
            <Text textStyle="accent">
              {step === 0
                ? investigationModalProps.title
                : t("Investigation Result")}
            </Text>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <StepsRoot step={step} defaultStep={0} count={2}>
            <StepsList>
              <StepsItem index={0} title={t("Pick A Player")} />
              <StepsItem index={1} title={t("Results")} />
            </StepsList>

            <StepsContent index={0}>
              <PlayerList
                selectedPlayer={selectedPlayerRoleId}
                players={alivePlayers}
                onPlayerSelect={(playerRoleId) => {
                  setSelectedPlayerRoleId(playerRoleId);
                }}
              />
            </StepsContent>
            <StepsContent index={1}>
              {investigationModalProps.resultNode}
            </StepsContent>
          </StepsRoot>
        </DialogBody>
        <DialogFooter>
          <DialogContext>
            {(context) => (
              <>
                {step === 0 && (
                  <Button
                    disabled={!selectedPlayerRoleId}
                    onClick={onSubmit}
                    size="sm"
                  >
                    {t("game.roleAction.investigate.label")}
                  </Button>
                )}
                {step === 1 && (
                  <Button
                    onClick={() => {
                      context.setOpen(false);
                    }}
                    size="sm"
                  >
                    {t("Close")}
                  </Button>
                )}
              </>
            )}
          </DialogContext>
        </DialogFooter>
      </DialogContent>
    </DialogRoot>
  );
};

export default InvestigationModal;
