import {
  DialogActionTrigger,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogRoot,
  DialogTitle,
  DialogTrigger,
} from "../ui/dialog";
import { useLeaveRoom } from "@/hooks/useLeaveRoom";
import { useNavigate } from "@tanstack/react-router";
import { Button } from "../ui/button";
import { useRoomId } from "@/hooks/useRoomId";
import { IconLogout2 } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";
import { Alert, Stack, Text } from "@chakra-ui/react";
import { useIsModerator } from "@/hooks/useIsModerator";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { useToaster } from "@/hooks/ui/useToaster";

export const LeaveRoomButton = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const navigate = useNavigate();
  const { showToast } = useToaster();
  const { getConnectionId } = useSocketConnection({});

  const { mutate: leaveRoomMutate, isPending: isLeavingRoom } = useLeaveRoom({
    onSuccess: async () => {
      await navigate({ to: "/" });
    },
    onError: async (e) => {
      // The server refuses to remove a player who holds a role in a running game — most
      // likely the game started while this dialog was open.
      showToast({
        type: "error",
        title: t("Can't Leave Room"),
        description: e.message,
        duration: 3000,
        withDismissButton: true,
      });
    },
  });
  const { data: isModerator } = useIsModerator(roomId);
  return (
    <DialogRoot role="alertdialog">
      <DialogTrigger asChild>
        <Button size="sm" variant="subtle" colorPalette="red">
          <IconLogout2 /> {t("lobby.button.leaveRoom")}
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            <Text textStyle="accent" fontSize="xl">
              {t("lobby.leaveRoomModal.title")}
            </Text>
          </DialogTitle>
        </DialogHeader>
        <DialogBody>
          <Stack gap={2}>
            {isModerator ? (
              <Alert.Root size="sm" status="warning">
                <Alert.Indicator />
                <Alert.Content>
                  <Alert.Title>
                    {t("lobby.leaveRoomModal.moderatorWarning.title")}
                  </Alert.Title>
                  <Alert.Description>
                    {t("lobby.leaveRoomModal.moderatorWarning.description")}
                  </Alert.Description>
                </Alert.Content>
              </Alert.Root>
            ) : null}
            <Text fontSize="xs">{t("lobby.leaveRoomModal.description")}</Text>
          </Stack>
        </DialogBody>
        <DialogFooter>
          <DialogActionTrigger asChild>
            <Button variant="outline">
              {t("lobby.leaveRoomModal.button.cancel")}
            </Button>
          </DialogActionTrigger>
          <Button
            loading={isLeavingRoom}
            disabled={isLeavingRoom}
            onClick={() => {
              leaveRoomMutate({ roomId, connectionId: getConnectionId() });
            }}
            colorPalette="red"
          >
            {t("lobby.leaveRoomModal.button.leaveRoom")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </DialogRoot>
  );
};
