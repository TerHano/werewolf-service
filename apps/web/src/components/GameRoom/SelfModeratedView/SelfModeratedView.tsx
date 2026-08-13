import { Stack } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { useCallback, useEffect } from "react";
import { useTranslation } from "react-i18next";
import { useQueryClient } from "@tanstack/react-query";
import { IconBell } from "@tabler/icons-react";
import { useRoomId } from "@/hooks/useRoomId";
import { useMyRole, myRoleQueryKey } from "@/hooks/useMyRole";
import { useGamePlayers, gamePlayersQueryKey } from "@/hooks/useGamePlayers";
import { useNightState, nightStateQueryKey } from "@/hooks/useNightState";
import { useIsModerator } from "@/hooks/useIsModerator";
import { useStartNight } from "@/hooks/useStartNight";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { useToaster } from "@/hooks/ui/useToaster";
import { useRoles } from "@/hooks/useRoles";
import { Skeleton } from "@/components/ui-addons/skeleton";
import { NightPhaseView } from "@/components/GameRoom/PlayerView/NightPhaseView";
import { PlayerRoleCard } from "@/components/GameRoom/PlayerView/PlayerRoleCard";
import { SelfModeratedDay } from "./SelfModeratedDay";
import { playNightOverCue, primeNightCue } from "@/util/nightCue";
import { EnableNotificationsCard } from "./EnableNotificationsCard";
import { ModeratorBadgeCard } from "./ModeratorBadgeCard";
import { SpectatorCard } from "./SpectatorCard";

/**
 * The whole game for a room the server moderates: everybody plays, including the host.
 *
 * There is no separate moderator screen here — the badge holder sees the same night as anyone
 * else, and only gains the controls that run the day.
 */
export const SelfModeratedView = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const queryClient = useQueryClient();
  const { showToast } = useToaster();
  const { getRole } = useRoles();

  const { data: nightState, isLoading: isNightStateLoading } =
    useNightState(roomId);
  const { data: myRole, isLoading: isMyRoleLoading } = useMyRole(roomId);
  const { data: players, isLoading: arePlayersLoading } = useGamePlayers(roomId);
  const { data: isModerator } = useIsModerator(roomId);

  const refreshNight = useCallback(() => {
    queryClient.invalidateQueries({ queryKey: [nightStateQueryKey, roomId] });
    queryClient.invalidateQueries({ queryKey: [gamePlayersQueryKey, roomId] });
    queryClient.invalidateQueries({ queryKey: [myRoleQueryKey, roomId] });
    queryClient.invalidateQueries({
      queryKey: ["latest-player-deaths", roomId],
    });
    queryClient.invalidateQueries({ queryKey: ["queued-action"] });
    queryClient.invalidateQueries({ queryKey: ["player-role-actions"] });
  }, [queryClient, roomId]);

  const { mutate: startNight, isPending: isStartingNight } = useStartNight({
    onSuccess: async () => refreshNight(),
  });

  // Audio needs a user gesture to become usable, so get it ready well before the first night
  // ends rather than discovering at daybreak that the browser will not play anything.
  useEffect(() => {
    const prime = () => primeNightCue();
    window.addEventListener("pointerdown", prime, { once: true });
    return () => window.removeEventListener("pointerdown", prime);
  }, []);

  useSocketConnection({
    onNightStarted: refreshNight,
    onNightAdvanced: refreshNight,
    onNightResolved: () => {
      // Everyone learns daybreak at the same instant, so a shared cue gives nothing away — and
      // it is the only way to know the night is over without looking at your screen.
      playNightOverCue();
      refreshNight();
    },
    onStepExtended: refreshNight,
    onDayOrTimeUpdated: refreshNight,
    // The badge changes hands on the first death, so the holder's controls have to appear
    // without a reload.
    onModeratorUpdated: refreshNight,
    // The in-app twin of the push notification. This one fires whenever the app is open;
    // push covers the far more common case of a phone lying face-down on the table.
    onYourTurn: () => {
      showToast({
        title: t("game.night.yourTurn.title"),
        description: t("game.night.yourTurn.description"),
        type: "info",
        icon: <IconBell size={12} />,
        duration: 8000,
      });
    },
  });

  if (isNightStateLoading || isMyRoleLoading || arePlayersLoading || !nightState) {
    return <Skeleton loading height={240} />;
  }

  const isNightCallRunning = nightState.isNightCallRunning;
  const isDead = myRole != null && !myRole.isAlive;
  const hasBadge = isModerator === true;
  const roleInfo = myRole ? getRole(myRole.role) : undefined;
  // Between dealing and the first step, players are still looking at their cards.
  const showRoleCard = !nightState.isDay && !isNightCallRunning && roleInfo;

  if (nightState.isDay) {
    return (
      <Stack gap={4}>
        {/* Offered during the day on purpose: asking for notification permission mid-night
            would put a system dialog over the one screen a player needs. */}
        <EnableNotificationsCard />
        {hasBadge && (
          <ModeratorBadgeCard isNightCallRunning={false} onExtended={refreshNight} />
        )}
        {isDead && myRole && (
          <SpectatorCard myRole={myRole} players={players ?? []} />
        )}
        <SelfModeratedDay
          players={players ?? []}
          myRole={myRole ?? null}
          canRunTheDay={isModerator === true}
          onChanged={refreshNight}
        />
      </Stack>
    );
  }

  return (
    <Stack gap={4}>
      {/* Also shown while players are still looking at their cards, before the first step. */}
      {!isNightCallRunning && <EnableNotificationsCard />}
      {hasBadge && (
        <ModeratorBadgeCard
          isNightCallRunning={isNightCallRunning}
          onExtended={refreshNight}
        />
      )}
      {showRoleCard && <PlayerRoleCard roleInfo={roleInfo} />}
      <NightPhaseView
        nightState={nightState}
        myRole={myRole ?? null}
        players={players ?? []}
      />
      {isDead && myRole && <SpectatorCard myRole={myRole} players={players ?? []} />}
      {isModerator && !isNightCallRunning && (
        <Button
          colorPalette="blue"
          loading={isStartingNight}
          onClick={() => startNight({ roomId })}
        >
          {t("game.night.begin")}
        </Button>
      )}
    </Stack>
  );
};

export default SelfModeratedView;
