import { Flex, Image, SimpleGrid, Stack } from "@chakra-ui/react";
import { Button } from "../../../ui/button";
import { useRoomId } from "@/hooks/useRoomId";
import {
  StepsContent,
  StepsItem,
  StepsList,
  StepsRoot,
} from "../../../ui/steps";
import { RoleInfo, useRoles } from "@/hooks/useRoles";
import { lazy, useMemo, useState } from "react";
import { PlayerRoleActionDto } from "@/dto/PlayerRoleActionDto";
import { useAllPlayerRoles } from "@/hooks/useAllPlayerRoles";
import { NightCompletedCard } from "./NightCompletedCard";
import { useAllQueuedActions } from "@/hooks/useAllQueuedActions";
import {
  IconArrowLeft,
  IconArrowRight,
  IconSunFilled,
} from "@tabler/icons-react";
import { Role } from "@/enum/Role";
import werewolfImg from "@/assets/icons/roles/werewolf-color.png";
import { useTranslation } from "react-i18next";

const PlayerActionCard = lazy(
  () => import("./PlayerActionCard/PlayerActionCard")
);

const WerewolfPlayersActionCard = lazy(
  () => import("./PlayerActionCard/WerewolfPlayersActionCard")
);

export interface PlayerRoleWithDetails extends PlayerRoleActionDto {
  roleInfo: RoleInfo;
}

export const NightCall = () => {
  const roomId = useRoomId();
  const [currentStep, setCurrentStep] = useState<number>(0);
  const { t } = useTranslation();

  const { data: allPlayerRoles } = useAllPlayerRoles(roomId);
  const roles = allPlayerRoles?.map((playerRole) => playerRole.role);
  const { getRole, getRoles } = useRoles();
  const roleDetails = getRoles(roles ?? []);
  const { data: allQueuedActions } = useAllQueuedActions(roomId);
  const playerRolesWithDetails = useMemo<PlayerRoleWithDetails[]>(() => {
    if (!allPlayerRoles || !roleDetails) {
      return [];
    }
    return allPlayerRoles
      .map<PlayerRoleWithDetails>((assignedRole) => {
        const roleInfo = getRole(assignedRole.role);
        if (!roleInfo) {
          throw new Error(`Role info not found for role: ${assignedRole.role}`);
        }
        return {
          ...assignedRole,
          roleInfo,
        };
      })
      .sort(
        (a, b) => a.roleInfo.roleCallPriority - b.roleInfo.roleCallPriority
      );
  }, [allPlayerRoles, getRole, roleDetails]);

  const rolesToCallExcludingWerewolves = playerRolesWithDetails.filter(
    (assignedRole) =>
      assignedRole.roleInfo.showInModeratorRoleCall &&
      assignedRole.role !== Role.WereWolf
  );

  //Adding werewolf and 'Go to sleep' card to count
  const nightCallLength = rolesToCallExcludingWerewolves.length + 1;

  return (
    <Stack height="auto" direction="column" width="100%" gap={4}>
      <StepsRoot
        step={currentStep}
        variant="subtle"
        size="lg"
        defaultValue={0}
        count={nightCallLength}
        gap={2}
      >
        <StepsList
          className="animate-fade-in-from-left"
          justifyContent="center"
        >
          <Flex wrap="wrap" gapY={2}>
            <StepsItem
              maxW="10rem"
              minW="6rem"
              onClick={() => setCurrentStep(0)}
              icon={<Image width="24px" src={werewolfImg} />}
              key={`item-werewolves`}
              index={0}
            />
            {rolesToCallExcludingWerewolves.map((playerRole, index) => {
              return (
                <StepsItem
                  maxW="10rem"
                  minW="6rem"
                  onClick={() => setCurrentStep(index + 1)}
                  icon={<Image width="24px" src={playerRole.roleInfo.imgSrc} />}
                  key={`item-${playerRole.id}`}
                  index={index + 1}
                  // title={playerRole.roleInfo.label}
                />
              );
            })}
            <StepsItem
              onClick={() => setCurrentStep(nightCallLength)}
              icon={<IconSunFilled />}
              key={`item-complete`}
              index={nightCallLength}
              // title={playerRole.roleInfo.label}
            />
          </Flex>
        </StepsList>
        <Stack
          height="80vh"
          width="100%"
          alignItems="center"
          justifyContent="start"
          gap={0}
        >
          <StepsContent width="100%" key={`content-werewolves`} index={0}>
            <WerewolfPlayersActionCard
              allPlayerDetails={playerRolesWithDetails}
              allQueuedActions={allQueuedActions ?? []}
            />
          </StepsContent>
          {rolesToCallExcludingWerewolves.map((playerRole, index) => {
            return (
              <StepsContent
                width="100%"
                key={`content-${playerRole.id}`}
                index={index + 1}
              >
                <>
                  <PlayerActionCard
                    playerDetails={playerRole}
                    allPlayerDetails={playerRolesWithDetails}
                    allQueuedActions={allQueuedActions ?? []}
                  />
                </>
              </StepsContent>
            );
          })}
          <StepsContent
            width="100%"
            key={`content-complete`}
            index={nightCallLength}
          >
            <NightCompletedCard />
          </StepsContent>

          <SimpleGrid columns={2} w="full">
            <Button
              borderTopRadius={0}
              borderRightRadius={0}
              disabled={currentStep === 0}
              onClick={() => setCurrentStep(currentStep - 1)}
              variant="subtle"
              size="md"
            >
              <IconArrowLeft />
              {t("Previous")}
            </Button>
            <Button
              borderLeftRadius={0}
              borderTopRadius={0}
              disabled={currentStep === nightCallLength}
              onClick={() => setCurrentStep(currentStep + 1)}
              variant="subtle"
              size="md"
            >
              {t("Next")}
              <IconArrowRight />
            </Button>
          </SimpleGrid>
        </Stack>
      </StepsRoot>
    </Stack>
  );
};
