import { useRoles } from "@/hooks/useRoles";
import { HStack, Separator, Stack, Text } from "@chakra-ui/react";
import { ActiveRolesList } from "./ActiveRolesList";
import { useMeasure } from "@uidotdev/usehooks";
import { RoleInfoList } from "../RoleInfoList";
import { useTranslation } from "react-i18next";
import { useState } from "react";
import { SegmentedControl } from "@/components/ui/segmented-control";
import {
  DrawerBackdrop,
  DrawerBody,
  DrawerCloseTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerRoot,
  DrawerTrigger,
} from "@/components/ui/drawer";
import { IconCards, IconUserUp } from "@tabler/icons-react";
import { Button } from "@/components/ui/button";
import { DrawerPlacementForMobileDesktop } from "@/util/drawer";
import { Role } from "@/enum/Role";

type ShowRoleControl = "active" | "all";

export const RoomRoleSettingsInfo = ({
  numberOfWerewolves,
  activeRoles,
}: {
  numberOfWerewolves: number;
  activeRoles: number[];
}) => {
  const { t } = useTranslation();
  const { data: allRoles, getRole } = useRoles();
  const werewolfRole = getRole(Role.WereWolf); // Assuming 1 is the Werewolf role ID
  const activeRolesData = allRoles?.filter((role) =>
    activeRoles.includes(role.roleName)
  );
  const [ref, { width }] = useMeasure();

  const [roleControl, setRoleControl] = useState<ShowRoleControl>("active");

  if (!werewolfRole) {
    throw new Error("Problem getting werewolf");
  }

  const roleList =
    roleControl === "all" ? allRoles : [werewolfRole, ...activeRolesData];

  return (
    <DrawerRoot
      onExitComplete={() => {
        setRoleControl("active");
      }}
      size={{ base: "full", md: "md" }}
      placement={DrawerPlacementForMobileDesktop}
    >
      <DrawerBackdrop />
      <DrawerTrigger asChild>
        <Button size="sm" w="full" variant="subtle" colorPalette="blue">
          <IconUserUp /> {t("button.viewSettings")}
        </Button>
      </DrawerTrigger>
      <DrawerContent>
        <DrawerCloseTrigger />
        <DrawerHeader>
          <HStack gap={1}>
            <IconCards size={18} />
            <Text fontWeight={500} fontSize="lg">
              {t("common.roleSettings")}
            </Text>
          </HStack>
        </DrawerHeader>
        <DrawerBody>
          <Stack ref={ref} mb="1rem" align="center" gap={3}>
            <ActiveRolesList
              widthOfContainer={width}
              numberOfWerewolves={numberOfWerewolves}
              activeRoles={activeRoles}
            />

            <Separator flex="1" />

            <SegmentedControl
              w="fit-content"
              value={roleControl}
              onValueChange={(e) => setRoleControl(e.value as ShowRoleControl)}
              items={[
                {
                  label: t("roleSettings.viewSettings.active"),
                  value: "active",
                },
                { label: t("roleSettings.viewSettings.all"), value: "all" },
              ]}
            />
            <RoleInfoList roles={roleList} />
          </Stack>
        </DrawerBody>
      </DrawerContent>
    </DrawerRoot>
  );
};

export default RoomRoleSettingsInfo;
