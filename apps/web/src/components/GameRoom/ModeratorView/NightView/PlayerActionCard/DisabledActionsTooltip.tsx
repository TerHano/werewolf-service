import {
  PopoverBody,
  PopoverContent,
  PopoverRoot,
  PopoverTitle,
  PopoverTrigger,
} from "@/components/ui/popover";
import { RoleActionDto } from "@/dto/RoleActionDto";
import { Stack, Button, Text, Badge, Group } from "@chakra-ui/react";
import { IconAlertTriangle } from "@tabler/icons-react";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";

export const DisabledActionsTooltip = ({
  actions,
}: {
  actions?: RoleActionDto[];
}) => {
  const { t } = useTranslation();

  const disabledActions = actions?.filter((action) => !action.enabled);

  const tooltipContent = useMemo(() => {
    return (
      <Stack mt={3}>
        {disabledActions?.map((action) => {
          return (
            <Group gap={1}>
              <Badge>{action.label}</Badge>-
              <Text fontSize="xs">{action.disabledReason}</Text>
            </Group>
          );
        })}
      </Stack>
    );
  }, [disabledActions]);

  if (disabledActions === undefined || disabledActions.length === 0) {
    return null;
  }

  return (
    <PopoverRoot positioning={{ placement: "top" }}>
      <PopoverTrigger asChild>
        <Button size="xs" variant="ghost" colorPalette="orange">
          <IconAlertTriangle />
        </Button>
      </PopoverTrigger>
      <PopoverContent>
        <PopoverBody>
          <PopoverTitle fontWeight="medium">
            {t("Disabled Actions")}
          </PopoverTitle>
          {tooltipContent}
        </PopoverBody>
      </PopoverContent>
    </PopoverRoot>
  );
};
