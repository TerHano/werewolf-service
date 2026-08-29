import { Badge, Card, HStack, Stack, Text } from "@chakra-ui/react";
import { IconUsersGroup } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";
import { useRoomId } from "@/hooks/useRoomId";
import { useLobbySeats } from "@/hooks/useLobbySeats";
import { Skeleton } from "../ui-addons/skeleton";

/**
 * Shows the room how close it is to being dealt: players who will get a card against the
 * number of cards the current role settings make.
 */
export const SeatCounter = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const {
    playersToDealTo,
    cardsInDeck,
    playersNeeded,
    extraVillagers,
    isLoading,
  } = useLobbySeats(roomId);

  const isReady = playersNeeded === 0;

  return (
    <Skeleton w="full" height="3rem" loading={isLoading}>
      <Card.Root size="sm" variant="subtle" w="full">
        <Card.Body p={3}>
          <Stack gap={0}>
            <HStack justify="space-between">
              <HStack gap={2}>
                <IconUsersGroup size={16} />
                <Text fontSize="sm">
                  {isReady
                    ? t("lobby.seats.readyToStart")
                    : t("lobby.seats.morePlayersNeeded", {
                        count: playersNeeded,
                      })}
                </Text>
              </HStack>
              <Badge
                colorPalette={isReady ? "green" : "orange"}
                variant="surface"
                size="sm"
              >
                {t("lobby.seats.count", {
                  players: playersToDealTo,
                  cards: cardsInDeck,
                })}
              </Badge>
            </HStack>
            {isReady && extraVillagers > 0 ? (
              <Text fontSize="xs" color="gray.400">
                {t("lobby.seats.extraVillagers", { count: extraVillagers })}
              </Text>
            ) : null}
          </Stack>
        </Card.Body>
      </Card.Root>
    </Skeleton>
  );
};

export default SeatCounter;
