import { Card, Stack, Text } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { IconClockPlus, IconMicrophone } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";
import { useRoomId } from "@/hooks/useRoomId";
import { useExtendNightStep } from "@/hooks/useExtendNightStep";

interface ModeratorBadgeCardProps {
  /** Whether a night step is currently running, which is the only time extending applies. */
  isNightCallRunning: boolean;
  onExtended: () => void;
}

/**
 * The badge holder's controls.
 *
 * Deliberately sparse. The badge's real content is the job of narrating the table — "everyone
 * close your eyes, werewolves wake up" — which keeps a dozen people synchronised and needs no
 * software at all. Mechanically it is one button per day phase plus this one.
 *
 * Note what is absent: no role list, no count of who has yet to act, and no way to end a step
 * early. All three would leak. Knowing that a step has nobody left to act in is the same as
 * knowing that role is dead.
 */
export const ModeratorBadgeCard = ({
  isNightCallRunning,
  onExtended,
}: ModeratorBadgeCardProps) => {
  const { t } = useTranslation();
  const roomId = useRoomId();

  const { mutate: extendStep, isPending: isExtending } = useExtendNightStep({
    onSuccess: async () => onExtended(),
  });

  return (
    <Card.Root variant="subtle">
      <Card.Body>
        <Stack gap={3}>
          <Stack direction="row" align="center" gap={3}>
            <IconMicrophone />
            <Stack gap={0} flex="1">
              <Text textStyle="accent">{t("game.badge.title")}</Text>
              <Text color="dimmed" fontSize="sm">
                {t("game.badge.description")}
              </Text>
            </Stack>
          </Stack>

          {isNightCallRunning && (
            <Button
              size="sm"
              variant="subtle"
              loading={isExtending}
              onClick={() => extendStep({ roomId })}
            >
              <IconClockPlus size={16} />
              {t("game.badge.extend")}
            </Button>
          )}
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};
