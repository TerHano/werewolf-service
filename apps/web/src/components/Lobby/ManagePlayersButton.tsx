import { PlayerDto } from "@/dto/PlayerDto";
import { Button } from "../ui/button";
import {
  DrawerBackdrop,
  DrawerBody,
  DrawerCloseTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerTrigger,
} from "../ui/drawer";
import { useRoomId } from "@/hooks/useRoomId";
import {
  Alert,
  DrawerRootProvider,
  IconButton,
  Image,
  Separator,
  Stack,
  Text,
  useDrawer,
} from "@chakra-ui/react";
import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { useTranslation } from "react-i18next";
import { IconKarate, IconSpeakerphone, IconUserCog } from "@tabler/icons-react";
import { useUpdateModerator } from "@/hooks/useUpdateModerator";
import { useCallback, useState } from "react";
import { useKickPlayer } from "@/hooks/useKickPlayer";
import { useToaster } from "@/hooks/ui/useToaster";
import { DrawerPlacementForMobileDesktop } from "@/util/drawer";

type PendingAction = "kick" | "makeModerator" | null;

export const ManagePlayersButton = ({ player }: { player: PlayerDto }) => {
  const { t } = useTranslation();
  const drawer = useDrawer();
  const roomId = useRoomId();
  const { showToast } = useToaster();
  const { getAvatarImageSrcForIndex } = usePlayerAvatar();

  // Handing the room over cannot be undone by the player doing it — the update-moderator
  // endpoint requires being the moderator — so both actions confirm before they fire.
  const [pendingAction, setPendingAction] = useState<PendingAction>(null);

  const { mutate: updateModeratorMutate, isPending: isUpdatingModerator } =
    useUpdateModerator({
    onSuccess: async () => {
      drawer.setOpen(false);
    },
  });

  const { mutate: kickPlayerMutate, isPending: isKickingPlayer } = useKickPlayer({
    onSuccess: async () => {
      drawer.setOpen(false);
    },
    onError: async (e) => {
      // The server refuses to remove a player who holds a role in a running game.
      showToast({
        type: "error",
        title: t("Can't Kick Player"),
        description: e.message,
        duration: 3000,
        withDismissButton: true,
      });
      drawer.setOpen(false);
    },
  });

  const onUpdateModerator = useCallback(
    (playerId: number) => {
      updateModeratorMutate({
        newModeratorPlayerRoomId: playerId,
        roomId,
      });
    },
    [roomId, updateModeratorMutate]
  );

  const onKickPlayer = useCallback(
    (playerId: number) => {
      kickPlayerMutate({
        playerRoomIdToKick: playerId,
        roomId,
      });
    },
    [kickPlayerMutate, roomId]
  );
  return (
    <DrawerRootProvider
      value={drawer}
      onExitComplete={() => setPendingAction(null)}
      lazyMount
      unmountOnExit
      size="sm"
      placement={DrawerPlacementForMobileDesktop}
    >
      <DrawerBackdrop />
      <DrawerTrigger asChild>
        <IconButton
          size="sm"
          borderRadius="full"
          variant="subtle"
          colorPalette="blue"
        >
          <IconUserCog />
        </IconButton>
      </DrawerTrigger>
      <DrawerContent borderRadius="sm">
        <DrawerHeader>
          <Stack align="center" direction="row" gap={1}>
            <Text>{t("lobby.managePlayer.manage")}</Text>
            <Text fontWeight="semibold">{player.nickname}</Text>
            <Image
              src={getAvatarImageSrcForIndex(player.avatarIndex)}
              width="2rem"
            />
          </Stack>
        </DrawerHeader>
        <DrawerBody>
          {pendingAction === null ? (
            <Stack mb={5} gap={4}>
              <Button
                onClick={() => setPendingAction("kick")}
                variant="subtle"
                colorPalette="red"
              >
                <IconKarate />
                <Text fontSize="xs">{t("lobby.managePlayer.button.kick")}</Text>
              </Button>
              {/* One action removes them, the other hands them the room. Worth a beat of
                  separation so a mis-tap doesn't land on the wrong one. */}
              <Separator />
              <Button
                onClick={() => setPendingAction("makeModerator")}
                colorPalette="orange"
                variant="subtle"
              >
                <IconSpeakerphone />
                <Text fontSize="xs">
                  {t("lobby.managePlayer.button.makeModerator")}
                </Text>
              </Button>
            </Stack>
          ) : (
            <Stack mb={5} gap={4}>
              <Alert.Root
                size="sm"
                status={pendingAction === "kick" ? "error" : "warning"}
              >
                <Alert.Indicator />
                <Alert.Content>
                  <Alert.Title>
                    {t(`lobby.managePlayer.confirm.${pendingAction}.title`, {
                      nickname: player.nickname,
                    })}
                  </Alert.Title>
                  <Alert.Description>
                    {t(
                      `lobby.managePlayer.confirm.${pendingAction}.description`
                    )}
                  </Alert.Description>
                </Alert.Content>
              </Alert.Root>
              <Stack direction="row" gap={2}>
                <Button
                  flex="1"
                  variant="outline"
                  disabled={isKickingPlayer || isUpdatingModerator}
                  onClick={() => setPendingAction(null)}
                >
                  {t("lobby.managePlayer.confirm.cancel")}
                </Button>
                {pendingAction === "kick" ? (
                  <Button
                    flex="1"
                    colorPalette="red"
                    loading={isKickingPlayer}
                    onClick={() => {
                      void onKickPlayer(player.id);
                    }}
                  >
                    <IconKarate />
                    <Text fontSize="xs">
                      {t("lobby.managePlayer.button.kick")}
                    </Text>
                  </Button>
                ) : (
                  <Button
                    flex="1"
                    colorPalette="orange"
                    loading={isUpdatingModerator}
                    onClick={() => {
                      void onUpdateModerator(player.id);
                    }}
                  >
                    <IconSpeakerphone />
                    <Text fontSize="xs">
                      {t("lobby.managePlayer.button.makeModerator")}
                    </Text>
                  </Button>
                )}
              </Stack>
            </Stack>
          )}
        </DrawerBody>
        <DrawerCloseTrigger />
      </DrawerContent>
    </DrawerRootProvider>
  );
};
export default ManagePlayersButton;
