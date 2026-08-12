import { RoleActionDto } from "@/dto/RoleActionDto";
import { Group, Stack, Text } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { DisabledActionsTooltip } from "./DisabledActionsTooltip";
import { ActionModalBtnTrigger } from "../ActionModals/ActionModalBtnTrigger";

export const ActionButtonList = ({
  playerRoleId,
  actions,
  isVisible,
}: {
  playerRoleId?: number;
  actions?: RoleActionDto[];
  isVisible: boolean;
}) => {
  const { t } = useTranslation();

  return (
    <Stack
      className="animate-fade-in-from-bottom"
      animationDelay="slow"
      style={{ display: isVisible ? "inherit" : "none" }}
    >
      <Group>
        <Text fontSize="lg" textStyle="accent">
          {t("game.chooseAPlayerTo")}
        </Text>
        <DisabledActionsTooltip actions={actions} />
      </Group>
      <Group justifyContent="center">
        {actions?.map((action) => {
          return (
            <ActionModalBtnTrigger
              key={action.label}
              action={action}
              playerRoleId={playerRoleId}
            />
          );
        })}
      </Group>
    </Stack>
  );
};
