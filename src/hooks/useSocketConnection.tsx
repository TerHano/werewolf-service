import { useCallback, useContext, useEffect, useRef } from "react";
import { AddEditPlayerDetailsDto } from "@/dto/AddEditPlayerDetailsDto";
import { APIResponse } from "@/dto/APIResponse";
import { SocketContext } from "@/context/SocketContext";
import { HubConnectionState } from "@microsoft/signalr";
import { PlayerDto } from "@/dto/PlayerDto";

interface UseSocketConnection {
  onLobbyUpdated?: () => void;
  onGameStateChanged?: () => void;
  onModeratorUpdated?: (moderator: PlayerDto) => void;
  onPlayerKicked?: (kickedPlayerId: number) => void;
  onReconnect?: () => void;
  onRoomRoleSettingsUpdated?: () => void;
  onDayOrTimeUpdated?: () => void;
  onWinConditionMet?: () => void;
  onGameRestart?: () => void;
}

export const useSocketConnection = ({
  onLobbyUpdated,
  onModeratorUpdated,
  onGameStateChanged,
  onPlayerKicked,
  onReconnect,
  onRoomRoleSettingsUpdated,
  onDayOrTimeUpdated,
  onWinConditionMet,
  onGameRestart,
}: UseSocketConnection) => {
  const connection = useContext(SocketContext);
  if (connection == null) {
    throw new Error("Socket connection not set");
  }

  useEffect(() => {
    // if (connection.state === HubConnectionState.Connected) {
    if (onLobbyUpdated) {
      connection.on("PlayersInLobbyUpdated", onLobbyUpdated);
    }
    if (onModeratorUpdated) {
      connection.on("ModeratorUpdated", onModeratorUpdated);
    }
    if (onPlayerKicked) {
      connection.on("PlayerKicked", onPlayerKicked);
    }
    if (onRoomRoleSettingsUpdated) {
      connection.on("RoomRoleSettingsUpdated", onRoomRoleSettingsUpdated);
    }
    if (onGameStateChanged) {
      connection.on("GameState", onGameStateChanged);
    }
    if (onDayOrTimeUpdated) {
      connection.on("DayTimeUpdated", onDayOrTimeUpdated);
    }
    if (onWinConditionMet) {
      connection.on("WinConditionMet", onWinConditionMet);
    }
    if (onGameRestart) {
      connection.on("GameRestart", onGameRestart);
    }

    return () => {
      if (onLobbyUpdated) {
        connection.off("PlayersInLobbyUpdated", onLobbyUpdated);
      }
      if (onModeratorUpdated) {
        connection.off("ModeratorUpdated", onModeratorUpdated);
      }
      if (onRoomRoleSettingsUpdated) {
        connection.off("RoomRoleSettingsUpdated", onRoomRoleSettingsUpdated);
      }
      if (onPlayerKicked) {
        connection.off("PlayerKicked", onPlayerKicked);
      }
      if (onGameStateChanged) {
        connection.off("GameState", onGameStateChanged);
      }
      if (onDayOrTimeUpdated) {
        connection.off("DayTimeUpdated", onDayOrTimeUpdated);
      }
      if (onWinConditionMet) {
        connection.off("WinConditionMet", onWinConditionMet);
      }
      if (onGameRestart) {
        connection.off("GameRestart", onGameRestart);
      }
    };
  }, [
    connection,
    connection.state,
    onDayOrTimeUpdated,
    onGameRestart,
    onGameStateChanged,
    onLobbyUpdated,
    onModeratorUpdated,
    onPlayerKicked,
    onRoomRoleSettingsUpdated,
    onWinConditionMet,
  ]);

  // SignalR has no way to remove an onreconnected handler, so registering the caller's
  // callback directly would add another one on every render. Register a single stable
  // dispatcher per connection instead and route it through a ref holding the latest
  // callback.
  const onReconnectRef = useRef(onReconnect);
  onReconnectRef.current = onReconnect;

  useEffect(() => {
    connection.onreconnected(() => onReconnectRef.current?.());
  }, [connection]);

  const joinRoom = useCallback(
    (addEditPlayerDetails: AddEditPlayerDetailsDto) => {
      return connection.invoke<APIResponse>("JoinRoom", addEditPlayerDetails);
    },
    [connection]
  );

  const attemptReconnection = useCallback(() => {
    if (connection.state === HubConnectionState.Disconnected) {
      connection.start();
    }
  }, [connection]);

  const getConnectionId = useCallback(() => {
    if (connection.state === HubConnectionState.Connected) {
      return connection.connectionId!;
    } else {
      throw new Error("Connection is not established");
    }
  }, [connection]);

  return {
    getConnectionId,
    joinRoom,
    attemptReconnection,
    connectionState: connection.state,
  };
};
