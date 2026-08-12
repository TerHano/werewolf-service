import { PlayerDto } from "@/dto/PlayerDto";
import { usePlayers } from "@/hooks/usePlayers";
import { useRoomId } from "@/hooks/useRoomId";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import {
  Card,
  VStack,
  Image,
  SimpleGrid,
  Text,
  Separator,
  HStack,
} from "@chakra-ui/react";
import { useNavigate } from "@tanstack/react-router";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";
import { PlayerAvatar } from "./PlayerAvatar";
import emptyLobby from "@/assets/icons/lobby/lobby-empty.png";
import { ClipboardButton } from "../ui-addons/clipboard-button";
import React from "react";
import {
  Skeleton,
  SkeletonCircle,
  SkeletonComposed,
} from "@/components/ui-addons/skeleton";
import { IconCopyCheck } from "@tabler/icons-react";
import { useToaster } from "@/hooks/ui/useToaster";

export const PlayersSection = ({
  currentPlayer,
}: {
  currentPlayer?: PlayerDto;
}) => {
  const { t } = useTranslation();
  const { showToast } = useToaster();
  const roomId = useRoomId();
  const navigate = useNavigate();

  const {
    data: players,
    isLoading: isPlayersLoading,
    refetch: refetchPlayers,
  } = usePlayers(roomId);

  const onPlayerKicked = useCallback(
    (kickedPlayerId: number) => {
      if (currentPlayer?.id === kickedPlayerId) {
        showToast({
          title: t("lobby.kickedToast.title"),
          description: t("lobby.kickedToast.description"),
        });
        navigate({ to: "/" });
      } else {
        void refetchPlayers();
      }
    },
    [currentPlayer?.id, showToast, t, navigate, refetchPlayers]
  );

  useSocketConnection({
    onModeratorUpdated: useCallback(() => {
      void refetchPlayers();
    }, [refetchPlayers]),
    onLobbyUpdated: useCallback(() => {
      void refetchPlayers();
    }, [refetchPlayers]),
    onPlayerKicked,
  });

  const noPlayersInLobby = players?.length === 0;

  return (
    <>
      <HStack my={1}>
        <Separator flex="1" />
        <Text fontSize="xl" textStyle="accent" flexShrink="0">
          {noPlayersInLobby
            ? t("lobby.inviteFriends")
            : t(`lobby.playersWaiting`, { count: players?.length ?? 0 })}
        </Text>
        <Separator flex="1" />
      </HStack>
      <React.Fragment>
        <SkeletonComposed
          loading={isPlayersLoading}
          skeleton={<PlayerListSkeleton />}
        >
          {noPlayersInLobby ? (
            <VStack paddingY={3} gap={3}>
              <VStack gap={0}>
                <Image src={emptyLobby} height={32} width={32} />
                <Text lineHeight="shorter" fontSize="xl" textStyle="accent">
                  {t("lobby.inviteFriends")}
                </Text>
                <Text
                  textAlign="center"
                  lineHeight="shorter"
                  color="gray.400"
                  fontSize="lg"
                  textStyle="accent"
                >
                  {t("lobby.copyRoomCode")}
                </Text>
              </VStack>
              <ClipboardButton
                onCopy={() => {
                  showToast({
                    id: "roomIdCopy",
                    icon: <IconCopyCheck />,
                    type: "success",
                    title: t("lobby.roomIdCopied"),
                    withDismissButton: true,
                  });
                }}
                value={window.location.href}
              >
                {t("button.copyRoomUrl")}
              </ClipboardButton>
            </VStack>
          ) : null}

          <SimpleGrid
            justifyItems="center"
            gap={2}
            columns={{ base: 1, md: 3 }}
          >
            {players?.map((player, index) => (
              <PlayerAvatar
                currentPlayer={currentPlayer}
                key={player.id}
                player={player}
                className="animate-fade-in-from-bottom"
                css={{ animationDelay: `${index * 50}ms` }}
              />
            ))}
          </SimpleGrid>
        </SkeletonComposed>
      </React.Fragment>
    </>
  );
};

const PlayerListSkeleton = () => {
  return (
    <SimpleGrid justifyItems="center" gap={2} columns={{ base: 1, md: 3 }}>
      <PlayerSkeleton />
      <PlayerSkeleton />
      <PlayerSkeleton />
    </SimpleGrid>
  );
};

const PlayerSkeleton = () => {
  return (
    <Card.Root size="sm" w="full" variant="elevated">
      <Card.Body>
        <HStack justify="start" w="100%" gap={3}>
          <SkeletonCircle h="3rem" w="3rem" loading />
          <Skeleton height={8} w="100%" loading />
        </HStack>
      </Card.Body>
    </Card.Root>
  );
};

export default PlayersSection;
