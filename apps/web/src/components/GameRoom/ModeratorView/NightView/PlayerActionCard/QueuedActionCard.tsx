import { useRoleActionHelper } from "@/hooks/useRoleActionHelper";
import { Card, Stack, Button, Text, Group, Image } from "@chakra-ui/react";
import { IconArrowBackUp } from "@tabler/icons-react";
import { PlayerRoleWithDetails } from "../NightCall";
import { QueuedActionDto } from "@/dto/QueuedActionDto";
import { useDeleteQueuedAction } from "@/hooks/useDeleteQueuedAction";
import { useCallback } from "react";
import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { useTranslation } from "react-i18next";

export const QueuedActionCard = ({
  queuedAction,
  allPlayersDetails,
}: {
  queuedAction: QueuedActionDto;
  allPlayersDetails: PlayerRoleWithDetails[];
}) => {
  const { t } = useTranslation();
  const { getUndoActionText } = useRoleActionHelper();
  const { mutate: deleteQueuedAction } = useDeleteQueuedAction();
  const { getAvatarImageSrcForIndex } = usePlayerAvatar();

  const onDequeueAction = useCallback(
    (id: number) => {
      deleteQueuedAction(id);
    },
    [deleteQueuedAction]
  );
  const affectedPlayer = allPlayersDetails.find(
    (player) => player.id === queuedAction.affectedPlayerRoleId
  );

  if (!affectedPlayer) {
    return null;
  }
  return (
    <Stack
      className="animate-fade-in-from-bottom"
      animationDelay="moderate"
      direction="column"
      gap={1}
    >
      <Text fontSize="lg" textStyle="accent">
        {t("They decided to...")}
      </Text>
      <Card.Root py={1} pl={3} pr={1}>
        <Stack direction="row" alignItems="center" gap={2}>
          <Group gap={1}>
            <Text fontSize="sm">{getUndoActionText(queuedAction.action)}</Text>

            <Text fontSize="sm">{affectedPlayer.nickname}</Text>
            <Image
              w="24px"
              src={getAvatarImageSrcForIndex(affectedPlayer.avatarIndex)}
            />
          </Group>
          <Button
            variant="surface"
            size="xs"
            onClick={() => onDequeueAction(queuedAction.id)}
          >
            <IconArrowBackUp />
            {t("Undo")}
          </Button>
        </Stack>
      </Card.Root>
    </Stack>
  );
};
