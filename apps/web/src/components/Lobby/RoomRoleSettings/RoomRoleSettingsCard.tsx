import { useRoomId } from "@/hooks/useRoomId";
import { useRoomRoleSettings } from "@/hooks/useRoomRoleSettings";
import { Card, HStack, Text, VStack } from "@chakra-ui/react";
import { IconCards } from "@tabler/icons-react";
import { lazy, Suspense, useCallback, useMemo } from "react";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { useModerator } from "@/hooks/useModerator";
import { useCurrentPlayer } from "@/hooks/useCurrentPlayer";
import { ActiveRolesList } from "./ActiveRolesList";
import { useMeasure } from "@uidotdev/usehooks";
import { Skeleton } from "@/components/ui-addons/skeleton";
import { useTranslation } from "react-i18next";

const EditRoomRoleSettings = lazy(
  () => import("@/components/Lobby/RoomRoleSettings/EditRoomRoleSettings")
);

const RoomRoleSettingsInfo = lazy(
  () => import("@/components/Lobby/RoomRoleSettings/RoomRoleSettingsInfo")
);

export const RoomRoleSettingsCard = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const roomRoleSettingsQuery = useRoomRoleSettings(roomId);
  const {
    data: settings,
    isLoading: isRoomRoleSettingsLoading,
    refetch: refetchRoomRoleSettings,
  } = roomRoleSettingsQuery;
  const [ref, { width }] = useMeasure();
  const { data: currentModerator } = useModerator(roomId);
  const { data: currentPlayer } = useCurrentPlayer(roomId);

  const isModerator = useMemo(() => {
    if (!currentModerator || !currentPlayer) return false;
    return currentModerator.id === currentPlayer.id;
  }, [currentModerator, currentPlayer]);

  const onRoomRoleSettingsUpdated = useCallback(() => {
    void refetchRoomRoleSettings();
  }, [refetchRoomRoleSettings]);

  useSocketConnection({
    onRoomRoleSettingsUpdated,
  });

  const numberOfWerewolves = settings?.numberOfWerewolves ?? 1;

  return (
    <>
      <Card.Root zIndex={1} variant="elevated" w="full">
        <Card.Body p="1rem">
          <VStack ref={ref} w="100%" alignItems="start">
            <HStack gap={1}>
              <IconCards size={16} />
              <Text fontWeight="semibold" fontSize="sm">
                {t("common.roleSettings")}
              </Text>
            </HStack>

            <Skeleton
              w={width ?? "48px"}
              height="32px"
              loading={isRoomRoleSettingsLoading}
            >
              <ActiveRolesList
                widthOfContainer={width}
                numberOfWerewolves={numberOfWerewolves}
                activeRoles={settings?.selectedRoles ?? []}
              />
            </Skeleton>

            {isModerator ? (
              <Suspense fallback={<Skeleton loading h="full" w="full" />}>
                <EditRoomRoleSettings
                  roomRoleSettingsQuery={roomRoleSettingsQuery}
                />
              </Suspense>
            ) : (
              <Suspense fallback={<Skeleton loading h="full" w="full" />}>
                <RoomRoleSettingsInfo
                  numberOfWerewolves={numberOfWerewolves}
                  activeRoles={settings?.selectedRoles ?? []}
                />
              </Suspense>
            )}
          </VStack>
        </Card.Body>
      </Card.Root>
    </>
  );
};

export default RoomRoleSettingsCard;
