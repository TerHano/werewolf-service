import { Card, Progress, Stack, Text } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { lazy, Suspense } from "react";
import { MyRoleDto } from "@/dto/MyRoleDto";
import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { NightStateDto } from "@/dto/NightStateDto";
import { NightStep } from "@/enum/NightStep";
import { Role } from "@/enum/Role";
import { useStepCountdown } from "./useStepCountdown";
import { Skeleton } from "@/components/ui-addons/skeleton";

const NightActionPrompt = lazy(() => import("./NightActionPrompt"));

/** Which role acts in which step. Must match Role/NightStepRoles.cs on the server. */
const roleForStep: Partial<Record<NightStep, Role>> = {
  [NightStep.WerewolfKill]: Role.WereWolf,
  [NightStep.DoctorHeal]: Role.Doctor,
  [NightStep.DetectiveInvestigate]: Role.Detective,
  [NightStep.WitchAct]: Role.Witch,
  [NightStep.VigilanteShoot]: Role.Vigilante,
};

interface NightPhaseViewProps {
  nightState: NightStateDto;
  myRole: MyRoleDto | null;
  players: GamePlayerDto[];
}

export const NightPhaseView = ({
  nightState,
  myRole,
  players,
}: NightPhaseViewProps) => {
  const { t } = useTranslation();
  const secondsLeft = useStepCountdown(nightState.stepDeadline);

  const currentStep = nightState.currentStep;
  const isMyTurn =
    currentStep != null &&
    myRole != null &&
    myRole.isAlive &&
    roleForStep[currentStep] === myRole.role;

  if (currentStep == null) {
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

  const stepPosition = nightState.steps.indexOf(currentStep) + 1;
  const stepCount = nightState.steps.length;

  return (
    <Stack gap={4}>
      <Card.Root>
        <Card.Body>
          <Stack gap={2}>
            {/* Which step is running is public — it is called out loud at any table. What
                was chosen during it is not, and never appears here. */}
            <Text textStyle="accent" fontSize="lg" textAlign="center">
              {t(`game.night.step.${NightStep[currentStep]}`)}
            </Text>
            <Progress.Root
              size="sm"
              borderRadius="xl"
              value={stepCount > 0 ? (stepPosition / stepCount) * 100 : null}
            >
              <Progress.Track>
                <Progress.Range />
              </Progress.Track>
            </Progress.Root>
            <Text color="dimmed" fontSize="sm" textAlign="center">
              {t("game.night.stepProgress", {
                position: stepPosition,
                count: stepCount,
                seconds: secondsLeft ?? 0,
              })}
            </Text>
          </Stack>
        </Card.Body>
      </Card.Root>

      {isMyTurn ? (
        <Suspense fallback={<Skeleton loading height={200} />}>
          <NightActionPrompt myRole={myRole} players={players} />
        </Suspense>
      ) : (
        <Card.Root>
          <Card.Body>
            <Stack align="center" gap={2} py={4}>
              <Text textStyle="accent" fontSize="lg">
                {myRole?.isAlive === false
                  ? t("game.night.dead.title")
                  : t("game.night.notYourTurn.title")}
              </Text>
              <Text color="dimmed" textAlign="center">
                {myRole?.isAlive === false
                  ? t("game.night.dead.description")
                  : t("game.night.notYourTurn.description")}
              </Text>
            </Stack>
          </Card.Body>
        </Card.Root>
      )}
    </Stack>
  );
};
