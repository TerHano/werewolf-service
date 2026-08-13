import { Card, Progress, Stack, Text } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { lazy, Suspense } from "react";
import { MyRoleDto } from "@/dto/MyRoleDto";
import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { NightStateDto } from "@/dto/NightStateDto";
import { useStepCountdown } from "./useStepCountdown";
import { Skeleton } from "@/components/ui-addons/skeleton";
import { NightInProgress } from "./NightInProgress";

const NightActionPrompt = lazy(() => import("./NightActionPrompt"));

interface NightPhaseViewProps {
  nightState: NightStateDto;
  myRole: MyRoleDto | null;
  players: GamePlayerDto[];
}

/**
 * The night, from one player's seat.
 *
 * There is deliberately no shared view of which role is being called or how long is left. The
 * server only tells a player about the step they act in, so this component cannot show more
 * than it should even by accident — `currentStep` being non-null *is* the signal that it is
 * your turn.
 *
 * That opacity is what allows a turn to be ended early. If the room could watch each step, a
 * short step would mean somebody acted and a full-length one would mean nobody could — which
 * is to say, that role has died.
 */
export const NightPhaseView = ({
  nightState,
  myRole,
  players,
}: NightPhaseViewProps) => {
  const { t } = useTranslation();
  const secondsLeft = useStepCountdown(nightState.stepDeadline);

  if (!nightState.isNightCallRunning) {
    return (
      <Card.Root className="animate-fade-in-from-bottom">
        <Card.Body>
          <Stack align="center" gap={3} py={6}>
            <Text textStyle="accent" fontSize="xl">
              {t("game.night.waitingToBegin.title")}
            </Text>
            <Text color="dimmed" textAlign="center">
              {t("game.night.waitingToBegin.description")}
            </Text>
          </Stack>
        </Card.Body>
      </Card.Root>
    );
  }

  const isMyTurn = nightState.currentStep !== null;

  // Everyone who is not acting sees exactly this, whether they are alive, dead, or simply not
  // called tonight. No step, no clock, nothing to read the game from.
  if (!isMyTurn) {
    const isDead = myRole !== null && !myRole.isAlive;
    return (
      <NightInProgress
        title={isDead ? t("game.night.dead.title") : t("game.night.inProgress.title")}
        description={
          isDead
            ? t("game.night.dead.description")
            : t("game.night.inProgress.description")
        }
      />
    );
  }

  return (
    <Stack gap={4}>
      <Card.Root>
        <Card.Body>
          <Stack gap={2}>
            <Text textStyle="accent" fontSize="lg" textAlign="center">
              {nightState.hasLockedIn
                ? t("game.night.lockedIn.title")
                : t("game.night.yourTurn.title")}
            </Text>
            {secondsLeft !== null && (
              <>
                <Progress.Root size="sm" borderRadius="xl" value={null} />
                <Text color="dimmed" fontSize="sm" textAlign="center">
                  {t("game.night.secondsLeft", { seconds: secondsLeft })}
                </Text>
              </>
            )}
          </Stack>
        </Card.Body>
      </Card.Root>

      {nightState.hasLockedIn ? (
        <Card.Root>
          <Card.Body>
            <Stack align="center" gap={2} py={4}>
              <Text color="dimmed" textAlign="center">
                {t("game.night.lockedIn.description")}
              </Text>
            </Stack>
          </Card.Body>
        </Card.Root>
      ) : (
        myRole && (
          <Suspense fallback={<Skeleton loading height={200} />}>
            <NightActionPrompt myRole={myRole} players={players} />
          </Suspense>
        )
      )}
    </Stack>
  );
};
