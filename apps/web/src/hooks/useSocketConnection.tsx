import { useCallback, useContext, useEffect, useRef } from "react";
import { AddEditPlayerDetailsDto } from "@/dto/AddEditPlayerDetailsDto";
import { SocketResponse } from "@/dto/SocketResponse";
import { SocketContext } from "@/context/SocketContext";
import { HubConnectionState } from "@microsoft/signalr";
import { PlayerDto } from "@/dto/PlayerDto";
import { NightStep } from "@/enum/NightStep";

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
  onNightStarted?: (night: number) => void;
  /** Bare signal that the night moved on. Carries no step — see NightStateDto. */
  onNightAdvanced?: () => void;
  onNightResolved?: () => void;
  onStepExtended?: (step: NightStep, deadline: string) => void;
  /** Sent only to the players who act in the step that just began. */
  onYourTurn?: (step: NightStep, deadline: string) => void;
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
  onNightStarted,
  onNightAdvanced,
  onNightResolved,
  onStepExtended,
  onYourTurn,
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
    if (onNightStarted) {
      connection.on("NightStarted", onNightStarted);
    }
    if (onNightAdvanced) {
      connection.on("NightAdvanced", onNightAdvanced);
    }
    if (onNightResolved) {
      connection.on("NightResolved", onNightResolved);
    }
    if (onStepExtended) {
      connection.on("StepExtended", onStepExtended);
    }
    if (onYourTurn) {
      connection.on("YourTurn", onYourTurn);
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
      if (onNightStarted) {
        connection.off("NightStarted", onNightStarted);
      }
      if (onNightAdvanced) {
        connection.off("NightAdvanced", onNightAdvanced);
      }
      if (onNightResolved) {
        connection.off("NightResolved", onNightResolved);
      }
      if (onStepExtended) {
        connection.off("StepExtended", onStepExtended);
      }
      if (onYourTurn) {
        connection.off("YourTurn", onYourTurn);
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
    onNightStarted,
    onNightAdvanced,
    onNightResolved,
    onStepExtended,
    onYourTurn,
  ]);

  // SignalR has no way to remove an onreconnected handler, so registering the caller's
  // callback directly would add another one on every render. Register a single stable
  // dispatcher per connection instead and route it through a ref holding the latest
  // callback.
  const onReconnectRef = useRef(onReconnect);
  onReconnectRef.current = onReconnect;

  // Only register for callers that actually want reconnect notifications. Since the
  // handler can never be removed, registering a no-op dispatcher for every other caller
  // would still pile one up per mount. Call sites pass a fixed shape, so reading this
  // once on the first render is enough.
  const wantsReconnectRef = useRef(onReconnect !== undefined);

  useEffect(() => {
    if (!wantsReconnectRef.current) return;
    connection.onreconnected(() => onReconnectRef.current?.());
  }, [connection]);

  const joinRoom = useCallback(
    (addEditPlayerDetails: AddEditPlayerDetailsDto) => {
      return connection.invoke<SocketResponse>("JoinRoom", addEditPlayerDetails);
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
