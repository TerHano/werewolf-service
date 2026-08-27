import { GameRoom } from "@/components/GameRoom/GameRoom";
import { PhaseTransition } from "@/components/ui-addons/PhaseTransition";
import { AddEditPlayerModal } from "@/components/Lobby/AddEditPlayerModal";
import { Lobby } from "@/components/Lobby/Lobby";
import { Skeleton } from "@/components/ui-addons/skeleton";
import { RoomContext } from "@/context/RoomProvider";
import { AddEditPlayerDetailsDto } from "@/dto/AddEditPlayerDetailsDto";
import { GameState } from "@/enum/GameState";
import { useCheckRoom } from "@/hooks/useCheckRoom";
import { useGameState } from "@/hooks/useGameState";
import { useIsPlayerInRoom } from "@/hooks/useIsPlayerInRoom";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { useToaster } from "@/hooks/ui/useToaster";
import { SocketResponse } from "@/dto/SocketResponse";
import { getApi } from "@/util/api";
import { createFileRoute, redirect, useNavigate } from "@tanstack/react-router";
import { useDocumentTitle } from "@uidotdev/usehooks";
import { useCallback, useEffect } from "react";
import { useTranslation } from "react-i18next";

export const Route = createFileRoute("/room/$roomId")({
  component: RouteComponent,
  loader: async ({ params: { roomId } }) => {
    const doesRoomExistQuery = getApi<boolean>({
      url: `${import.meta.env.WEREWOLF_SERVER_URL}/api/room/check-room`,
      method: "POST",
      body: JSON.stringify({
        roomId: roomId,
      }),
    });
    const isPlayerInRoomQuery = getApi<boolean>({
      url: `${import.meta.env.WEREWOLF_SERVER_URL}/api/room/${roomId}/is-player-in-room`,
      method: "GET",
    });
    return await doesRoomExistQuery.then((exists) => {
      if (!exists) {
        throw redirect({
          to: "/",
        });
      }
      return isPlayerInRoomQuery;
    });
  },
});

function RouteComponent() {
  const { roomId } = Route.useParams();
  useDocumentTitle(`Werewolf Party | Room ${roomId}`);
  const _isPlayerAlreadyInRoomInitialData = Route.useLoaderData();
  const navigate = useNavigate();
  const { t } = useTranslation();
  const { showToast } = useToaster();

  // The hub reports join failures (missing/unknown room) in its response rather than by
  // faulting the invocation, so every JoinRoom call has to inspect the result itself.
  const handleJoinResponse = useCallback(
    (response: SocketResponse) => {
      if (response.success) {
        return true;
      }
      showToast({
        type: "error",
        title: t("room.joinRoomError.title"),
        description:
          response.errorMessage ?? t("room.joinRoomError.description"),
        withDismissButton: true,
      });
      navigate({ to: "/" });
      return false;
    },
    [navigate, showToast, t],
  );

  const { mutate: checkRoomMutate } = useCheckRoom({
    onSuccess: async (data) => {
      if (!data) {
        navigate({ to: "/" });
      } else {
        joinRoom({ roomId }).then((response) => {
          if (handleJoinResponse(response)) {
            void refetchIsPlayerInRoom();
          }
        });
      }
    },
  });
  const {
    data: isPlayerAlreadyInRoom,
    refetch: refetchIsPlayerInRoom,
    isLoading: isPlayerInRoomLoading,
  } = useIsPlayerInRoom({
    roomId,
    options: {
      initialData: _isPlayerAlreadyInRoomInitialData,
    },
  });

  const { joinRoom } = useSocketConnection({
    onReconnect: () => {
      checkRoomMutate({ roomId });
    },
  });

  useEffect(() => {
    if (isPlayerAlreadyInRoom) {
      void joinRoom({ roomId }).then(handleJoinResponse);
    }
  }, [handleJoinResponse, isPlayerAlreadyInRoom, joinRoom, roomId]);

  const joinRoomCb = useCallback(
    async (playerDetails: AddEditPlayerDetailsDto) => {
      return joinRoom(playerDetails).then((response) => {
        if (!handleJoinResponse(response)) {
          return;
        }
        return refetchIsPlayerInRoom().then(() => {
          return;
        });
      });
    },
    [handleJoinResponse, joinRoom, refetchIsPlayerInRoom],
  );

  return (
    <RoomContext.Provider value={{ roomId }}>
      <Skeleton loading={isPlayerInRoomLoading} h={0} w={0}>
        {isPlayerAlreadyInRoom ? (
          <Room roomId={roomId} />
        ) : (
          <AddEditPlayerModal submitCallback={joinRoomCb} />
        )}
      </Skeleton>
    </RoomContext.Provider>
  );
}

const Room = ({ roomId }: { roomId: string }) => {
  const {
    data: currentGameState,
    refetch: refetchGameState,
    isLoading: isGameStateLoading,
  } = useGameState(roomId);
  useSocketConnection({
    onGameStateChanged: () => {
      refetchGameState();
    },
  });

  if (isGameStateLoading) {
    return <Skeleton loading height={100} />;
  }

  return (
    <PhaseTransition
      phase={currentGameState}
      render={(phase) => (phase === GameState.Lobby ? <Lobby /> : <GameRoom />)}
    />
  );
};
