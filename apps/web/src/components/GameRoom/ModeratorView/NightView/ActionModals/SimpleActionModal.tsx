import { ActionType } from "@/enum/ActionType";
import { useCallback, useMemo, useState } from "react";
import {
  DialogBackdrop,
  DialogBody,
  DialogCloseTrigger,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { PlayerList } from "./PlayerList";
import { DialogRootProvider, Text, useDialog } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { useCreateUpdateQueuedAction } from "@/hooks/useCreateUpdateQueuedAction";
import { Button, ButtonProps } from "@/components/ui/button";
import { useRoomId } from "@/hooks/useRoomId";
import { PlayerRoleActionDto } from "@/dto/PlayerRoleActionDto";
import { useAllPlayerRoles } from "@/hooks/useAllPlayerRoles";
import { RoleActionDto } from "@/dto/RoleActionDto";

export interface SimpleActionModalProps {
  //isOpen: boolean;
  //onOpenChange: (isOpen: boolean) => void;
  playerRoleId?: number;
  action: RoleActionDto;
}

interface ModalContentProps {
  title: string;
  dialogTriggerBtn: ButtonProps;
  btnLabel: string;
  playerList?: PlayerRoleActionDto[];
}

export const SimpleActionModal = ({
  //isOpen,
  playerRoleId,
  // onOpenChange,
  action,
}: SimpleActionModalProps) => {
  const roomId = useRoomId();
  const { t } = useTranslation();
  const dialog = useDialog();

  //const close = useCallback(() => onOpenChange(false), [onOpenChange]);
  const { data: allPlayers } = useAllPlayerRoles(roomId);
  const [selectedPlayerId, setSelectedPlayerId] = useState<
    number | undefined
  >();
  const { mutate: createUpdateQueuedAction } = useCreateUpdateQueuedAction({
    onSuccess: async () => {
      dialog.setOpen(false);
    },
  });

  const onSubmit = useCallback(() => {
    if (selectedPlayerId) {
      createUpdateQueuedAction({
        roomId,
        playerRoleId: playerRoleId,
        affectedPlayerRoleId: selectedPlayerId,
        action: action.type,
      });
    }
  }, [
    action,
    createUpdateQueuedAction,
    playerRoleId,
    roomId,
    selectedPlayerId,
  ]);

  const modalContent = useMemo<ModalContentProps>(() => {
    const playerList = allPlayers?.filter((x) =>
      action.validPlayerIds.includes(x.id)
    );
    switch (action.type) {
      case ActionType.WerewolfKill:
        return {
          title: t("game.roleAction.werewolfKill.title"),
          btnLabel: t("game.roleAction.werewolfKill.label"),
          dialogTriggerBtn: {
            children: t("game.roleAction.werewolfKill.label"),
            colorPalette: "red",
          },
          playerList,
        };
      case ActionType.Revive:
        return {
          title: t("game.roleAction.heal.title"),
          btnLabel: t("game.roleAction.heal.label"),
          dialogTriggerBtn: {
            children: t("game.roleAction.heal.label"),
            colorPalette: "green",
          },
          playerList,
        };

      default:
        return {
          title: t("game.roleAction.kill.title"),
          btnLabel: t("game.roleAction.kill.label"),
          dialogTriggerBtn: {
            children: t("game.roleAction.kill.label"),
            colorPalette: "red",
          },
          playerList,
        };
    }
  }, [action, allPlayers, t]);

  return (
    <DialogRootProvider placement="center" value={dialog}>
      <DialogBackdrop />
      <DialogTrigger asChild>
        <Button
          {...modalContent.dialogTriggerBtn}
          disabled={!action.enabled}
          size="sm"
          minW="5rem"
        />
      </DialogTrigger>
      <DialogContent>
        <DialogCloseTrigger />
        <DialogHeader>
          <DialogTitle>
            <Text textStyle="accent">{modalContent.title}</Text>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <PlayerList
            players={modalContent.playerList ?? []}
            onPlayerSelect={setSelectedPlayerId}
            selectedPlayer={selectedPlayerId}
          />
        </DialogBody>
        <DialogFooter>
          <Button onClick={onSubmit} disabled={selectedPlayerId === undefined}>
            {modalContent.btnLabel}
          </Button>
        </DialogFooter>
      </DialogContent>
    </DialogRootProvider>
  );
};

export default SimpleActionModal;
