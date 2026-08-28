import { Card, Separator, Stack, Text } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { lazy, Suspense, useState } from "react";
import { useTranslation } from "react-i18next";
import { useRoomId } from "@/hooks/useRoomId";
import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { MyRoleDto } from "@/dto/MyRoleDto";
import { useVotePlayerOut } from "@/hooks/useVotePlayerOut";
import { useToaster } from "@/hooks/ui/useToaster";
import { PlayerList } from "@/components/GameRoom/ModeratorView/NightView/ActionModals/PlayerList";
import { Skeleton } from "@/components/ui-addons/skeleton";

const KilledPlayersBanner = lazy(
  () => import("@/components/GameRoom/ModeratorView/DayView/KilledPlayersBanner")
);

interface SelfModeratedDayProps {
  players: GamePlayerDto[];
  myRole: MyRoleDto | null;
  /** The badge holder runs the lynch; everyone else watches. */
  canRunTheDay: boolean;
  onChanged: () => void;
}

/**
 * The day in a self-moderated room.
 *
 * The village argues out loud and the app only records the outcome, so this is deliberately
 * thin — a list of the living and one button. It cannot reuse the moderator's ChoppingBlock,
 * which reads `all-player-roles`; that endpoint is closed during play now, and this view is
 * built from the roleless player list instead.
 */
export const SelfModeratedDay = ({
  players,
  canRunTheDay,
  onChanged,
}: SelfModeratedDayProps) => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const { showToast } = useToaster();
  const [selectedPlayer, setSelectedPlayer] = useState<number | undefined>();

  const { mutate: votePlayerOut, isPending: isVoting } = useVotePlayerOut({
    onSuccess: async () => {
      setSelectedPlayer(undefined);
      onChanged();
    },
    // A refused vote used to fail in silence, which is how a stale badge could look like a
    // dead button. Say what the server said, and resync — the usual reason is that the badge
    // has moved on and this screen has not caught up yet.
    onError: async (error) => {
      showToast({
        title: t("game.day.voteFailed"),
        description: error.message,
        type: "error",
      });
      onChanged();
    },
  });

  const alivePlayers = players.filter((player) => player.isAlive);
  const chosen = alivePlayers.find((player) => player.id === selectedPlayer);

  return (
    <Stack gap={6}>
      <Suspense fallback={<Skeleton loading height={80} />}>
        <KilledPlayersBanner />
      </Suspense>
      <Separator flex="1" />

      <Card.Root className="animate-fade-in-from-bottom">
        <Card.Body>
          <Stack gap={4}>
            <Text textStyle="accent" fontSize="xl">
              {t("game.choppingBlock.vote.title")}
            </Text>

            {canRunTheDay ? (
              <>
                <PlayerList
                  players={alivePlayers}
                  selectedPlayer={selectedPlayer}
                  onPlayerSelect={setSelectedPlayer}
                />
                <Stack direction={{ base: "column", sm: "row" }} gap={3}>
                  <Button
                    flex="1"
                    colorPalette="red"
                    disabled={selectedPlayer === undefined}
                    loading={isVoting}
                    onClick={() =>
                      votePlayerOut({ roomId, playerRoleId: selectedPlayer })
                    }
                  >
                    {/* Naming the target turns an irreversible tap into one you can check
                        before you make it — the vote lands the instant this is pressed. */}
                    {chosen
                      ? t("game.choppingBlock.vote.button.lynchNamed", {
                          name: chosen.nickname,
                        })
                      : t("game.choppingBlock.vote.button.lynch")}
                  </Button>
                  <Button
                    flex="1"
                    variant="subtle"
                    loading={isVoting}
                    onClick={() => votePlayerOut({ roomId })}
                  >
                    {t("game.choppingBlock.vote.button.abstain")}
                  </Button>
                </Stack>
              </>
            ) : (
              <Text color="dimmed" textAlign="center">
                {t("game.day.waitingForModerator")}
              </Text>
            )}
          </Stack>
        </Card.Body>
      </Card.Root>
    </Stack>
  );
};
