import { getApi } from "@/util/api";
import { getSessionCookie, setSessionCookie } from "@/util/cookie";
import {
  HubConnectionState,
  HubConnectionBuilder,
  LogLevel,
} from "@microsoft/signalr";
import {
  PropsWithChildren,
  useCallback,
  useEffect,
  useMemo,
  useRef,
  useState,
} from "react";
import { Button, Progress, Stack, Text } from "@chakra-ui/react";
import { IconCards, IconPlugConnectedX } from "@tabler/icons-react";
import { useDebounce } from "@uidotdev/usehooks";
import { useTranslation } from "react-i18next";
import { SocketContext } from "./SocketContext";
import { useToaster } from "@/hooks/ui/useToaster";

export const SocketProvider = ({ children }: PropsWithChildren) => {
  const { t } = useTranslation();
  const { showToast } = useToaster();
  const [_connectionState, setConnectionState] = useState<
    HubConnectionState | undefined
  >(undefined);

  const connectionState = useDebounce(_connectionState, 300);

  const connection = useMemo(() => {
    return new HubConnectionBuilder()
      .withUrl(`${import.meta.env.WEREWOLF_SERVER_URL}/Events`, {
        accessTokenFactory: async () => {
          const token = getSessionCookie();
          if (token == "") {
            const token = await getApi<string>({
              url: `${import.meta.env.WEREWOLF_SERVER_URL}/api/player/get-id`,
              method: "POST",
            });
            setSessionCookie(token);
            return token;
          }
          return token;
        },
      })
      .configureLogging(LogLevel.Error)
      .withAutomaticReconnect()
      .build();
  }, []);
  const dialogMessage = useMemo(() => {
    switch (connectionState) {
      case HubConnectionState.Connecting:
        return t("socket.status.connecting");
      case HubConnectionState.Reconnecting:
        return t("socket.status.reconnecting");
      case HubConnectionState.Disconnected:
        return t("socket.status.disconnected");
    }
  }, [connectionState, t]);

  const startConnection = useCallback(
    () =>
      connection
        .start()
        .then(() => {
          setConnectionState(connection.state);
          console.log("Connection started");
        })
        .catch(() => {
          setConnectionState(connection.state);
          console.log("Connection failed");
          showToast({
            title: t("Connection Error"),
            description: t(
              "Failed to connect to the server. Please try again later."
            ),
            type: "error",
            icon: <IconPlugConnectedX size={12} />,
            duration: 5000,
          });
        }),
    [connection, showToast, t]
  );
  // startConnection's identity changes whenever the toaster or translations change, and
  // it must not be a dependency of the effect below: SignalR cannot unregister
  // onreconnected/onreconnecting/onclose handlers, so re-running that effect would stack
  // up another three every time. Reach the latest version through a ref instead.
  const startConnectionRef = useRef(startConnection);
  startConnectionRef.current = startConnection;

  // `connection` is memoised with no dependencies, so this runs exactly once: the state
  // handlers are registered a single time, and the initial connection is started.
  useEffect(() => {
    const syncConnectionState = () => setConnectionState(connection.state);

    connection.onreconnected(syncConnectionState);
    connection.onreconnecting(syncConnectionState);
    connection.onclose(syncConnectionState);

    if (connection.state === HubConnectionState.Disconnected) {
      void startConnectionRef.current();
    }
  }, [connection]);

  return (
    <SocketContext.Provider value={connection}>
      {connectionState === HubConnectionState.Connected ? (
        children
      ) : (
        <Stack
          //   visibility={!isConnecting ? "hidden" : "visible"}
          className="animate-fade-in-from-bottom"
          paddingTop="20%"
          //  position="absolute"
          // left="20%"
          //top="50%"
          justify="center"
          align="center"
          gap={4}
          mt={4}
        >
          {connectionState === HubConnectionState.Disconnected ? (
            <>
              <IconPlugConnectedX size="128px" />
              <Text color="dimmed" fontSize="1rem">
                {dialogMessage}
              </Text>
              <Button
                size="sm"
                onClick={() => {
                  startConnection();
                }}
              >
                {t("socket.button.reconnect")}
              </Button>
            </>
          ) : (
            <>
              <IconCards size="128px" />
              <Progress.Root size="sm" borderRadius="xl" w="200px" value={null}>
                <Progress.Track>
                  <Progress.Range />
                </Progress.Track>
              </Progress.Root>
              <Text textStyle="accent" color="dimmed" fontSize="1em">
                {t("socket.status.connecting")}
              </Text>
            </>
          )}
        </Stack>
      )}
    </SocketContext.Provider>
  );
};
