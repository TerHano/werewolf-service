import { HStack, Stack, Text, SimpleGrid, Card } from "@chakra-ui/react";
import { useCurrentPlayer } from "@/hooks/useCurrentPlayer";
import { useTranslation } from "react-i18next";
import { useRoomId } from "@/hooks/useRoomId";
import { ClipboardIconButton } from "../ui-addons/clipboard-button";
import { lazy, Suspense, useCallback } from "react";
import { Button } from "../ui/button";
import { useStartGame } from "@/hooks/useStartGame";
import { useIsModerator } from "@/hooks/useIsModerator";
import { LeaveRoomButton } from "./LeaveRoomButton";
import { IconCopyCheck } from "@tabler/icons-react";
import { useToaster } from "@/hooks/ui/useToaster";
import { Skeleton, SkeletonCircle } from "../ui-addons/skeleton";

const RoomRoleSettingsCard = lazy(
  () => import("@/components/Lobby/RoomRoleSettings/RoomRoleSettingsCard")
);

const PlayersSection = lazy(() => import("@/components/Lobby/PlayerSection"));

const ModeratorCard = lazy(() => import("@/components/Lobby/ModeratorCard"));

export const Lobby = () => {
  const roomId = useRoomId();
  const { showToast } = useToaster();
  const { t } = useTranslation();
  const { data: isModerator } = useIsModerator(roomId);
  const { data: currentPlayer } = useCurrentPlayer(roomId);

  const { mutate: startGameMutate, isPending: isStartGamePending } =
    useStartGame({
      onError: async (e) => {
        showToast({
          type: "error",
          title: "Can't Start Game",
          description: e.message,
          duration: 2500,
          withDismissButton: true,
        });
      },
    });

  const onStartGame = useCallback(async () => {
    startGameMutate({ roomId });
  }, [roomId, startGameMutate]);

  return (
    <Card.Root w="full" p={3} variant="outline">
      <Stack w="full" gap={2}>
        <HStack justify="space-between">
          <HStack justifyContent="center">
            <Text>{t("common.roomId")}:</Text>
            <ClipboardIconButton
              value={window.location.href}
              label={roomId}
              iconButton={{
                colorPalette: "blue",
                size: "sm",
                variant: "subtle",
              }}
              onCopy={() =>
                showToast({
                  id: "roomIdCopy",
                  icon: <IconCopyCheck />,
                  duration: 1000,
                  type: "success",
                  title: t("lobby.roomIdCopied"),
                  withDismissButton: true,
                })
              }
            />
          </HStack>
          <LeaveRoomButton />
        </HStack>
        <Suspense
          fallback={
            <Card.Root size="sm" w="full" variant="elevated">
              <Card.Body>
                <HStack justify="start" w="100%" gap={3}>
                  <SkeletonCircle h="3rem" w="3rem" loading />
                  <Skeleton height={8} w="100%" loading />
                </HStack>
              </Card.Body>
            </Card.Root>
          }
        >
          <ModeratorCard currentPlayer={currentPlayer} />
        </Suspense>
        <Suspense
          fallback={
            <Card.Root>
              <Skeleton loading w="full" height={4} />
            </Card.Root>
          }
        >
          <RoomRoleSettingsCard />
        </Suspense>
        {isModerator ? (
          <SimpleGrid gap={2} columns={1}>
            <Button
              size="sm"
              width="100%"
              loading={isStartGamePending}
              onClick={onStartGame}
            >
              <Text fontSize="sm"> {t("lobby.button.startGame")}</Text>
            </Button>
          </SimpleGrid>
        ) : null}
        <Suspense
          fallback={
            <Card.Root size="sm" w="full" variant="elevated">
              <Card.Body>
                <HStack justify="start" w="100%" gap={3}>
                  <SkeletonCircle h="3rem" w="3rem" loading />
                  <Skeleton height={8} w="100%" loading />
                </HStack>
              </Card.Body>
            </Card.Root>
          }
        >
          <PlayersSection currentPlayer={currentPlayer} />
        </Suspense>
      </Stack>
    </Card.Root>
  );
};
