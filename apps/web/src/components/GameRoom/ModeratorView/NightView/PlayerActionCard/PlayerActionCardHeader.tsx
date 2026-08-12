import { useAnimationReset } from "@/hooks/useAnimationReset";
import { useTranslation } from "react-i18next";
import { Box, Group, Text } from "@chakra-ui/react";
import { useEffect } from "react";
import { IconEyeClosed, IconEye } from "@tabler/icons-react";

export const PlayerActionCardHeader = ({
  roleLabel,
  isActionQueued,
}: {
  roleLabel: string;
  isActionQueued: boolean;
}) => {
  const { t } = useTranslation();
  const { animation, resetAnimation } = useAnimationReset();
  useEffect(() => {
    resetAnimation();
  }, [isActionQueued, resetAnimation]);
  return (
    <Box
      className="animate-fade-in-from-bottom"
      // fontSize="xl"
      // textStyle="accent"
      animation={animation}
    >
      {isActionQueued ? (
        <Group>
          <Text fontSize="xl" textStyle="accent">
            {t(`game.roleCloseEyes`, { role: roleLabel })}
          </Text>
          <IconEyeClosed />
        </Group>
      ) : (
        <Group>
          <Text fontSize="xl" textStyle="accent">
            {t(`game.wakeUpRole`, { role: roleLabel })}
          </Text>
          <IconEye />
        </Group>
      )}
    </Box>
  );
};
