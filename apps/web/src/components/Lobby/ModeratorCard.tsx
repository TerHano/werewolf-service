import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import {
  Text,
  Image,
  Badge,
  Group,
  Stack,
  defineStyle,
} from "@chakra-ui/react";
import { Card } from "@chakra-ui/react/card";
import { useCallback } from "react";
import { useRoomId } from "@/hooks/useRoomId";
import { useTranslation } from "react-i18next";
import { useModerator } from "@/hooks/useModerator";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { AddEditPlayerModal } from "@/components/Lobby/AddEditPlayerModal";
import { useUpdateCurrentPlayerDetails } from "@/hooks/useUpdateCurrentPlayerDetails";
import { PlayerDto } from "@/dto/PlayerDto";
import { Skeleton, SkeletonCircle } from "../ui-addons/skeleton";
import { IconCrown } from "@tabler/icons-react";
import { useToaster } from "@/hooks/ui/useToaster";

export interface ModeratorCardProps {
  currentPlayer?: PlayerDto;
}

const moderatorIcon = <IconCrown />;

export const ModeratorCard = ({ currentPlayer }: ModeratorCardProps) => {
  const { t } = useTranslation();
  const { showToast } = useToaster();
  const roomId = useRoomId();
  const {
    data: currentModerator,
    isLoading: isModeratorLoading,
    refetch: refetchModerator,
  } = useModerator(roomId);
  const { mutateAsync: updatePlayerDetailsMutateAsync } =
    useUpdateCurrentPlayerDetails();
  const { getAvatarImageSrcForIndex } = usePlayerAvatar();

  const onModeratorUpdated = useCallback(
    (newModerator: PlayerDto) => {
      if (newModerator.id === currentPlayer?.id) {
        showToast({
          icon: moderatorIcon,
          title: t("You're In Charge!"),
          description: t("You are now the moderator!"),
          withDismissButton: true,
          type: "warning",
        });
      } else {
        showToast({
          title: t("New Moderator In Town!"),
          icon: moderatorIcon,
          withDismissButton: true,
          description: (
            <Group gap={1}>
              <Text fontStyle="italic" fontWeight="bold">
                {newModerator.nickname}
              </Text>
              <Text>{t("is now moderator")}</Text>
            </Group>
          ),
          type: "warning",
        });
      }
      refetchModerator();
    },
    [currentPlayer?.id, refetchModerator, showToast, t]
  );

  useSocketConnection({
    onModeratorUpdated,
    onLobbyUpdated: useCallback(() => {
      void refetchModerator();
    }, [refetchModerator]),
  });

  const isModeratorCurrentPlayer = currentModerator?.id === currentPlayer?.id;

  const ringCss = defineStyle({
    outlineWidth: "2px",
    outlineColor: "yellow.600",
    outlineStyle: "solid",
  });

  return (
    <Card.Root css={ringCss} variant="subtle" w="100%" size="sm">
      <Card.Body>
        <Stack direction="row" justify="space-between" align="center">
          <Stack direction="row" align="center" gap={2}>
            <SkeletonCircle size="3rem" loading={isModeratorLoading}>
              <Image
                width="3rem"
                src={getAvatarImageSrcForIndex(currentModerator?.avatarIndex)}
                alt="thing"
              />
            </SkeletonCircle>
            <Stack direction="column" align="start" gap={0}>
              <Group>
                <Badge colorPalette="yellow" size="sm">
                  <Text fontSize="sm">{t("common.moderator")}</Text>
                </Badge>
                {isModeratorCurrentPlayer ? (
                  <Badge colorPalette="blue" size="sm">
                    <Text fontSize="sm">{t("common.you")}</Text>
                  </Badge>
                ) : null}
              </Group>
              <Skeleton w="full" height={8} loading={isModeratorLoading}>
                <Text fontSize="lg" textStyle="accent">
                  {currentModerator?.nickname}
                </Text>
              </Skeleton>
            </Stack>
          </Stack>
          {isModeratorCurrentPlayer ? (
            <AddEditPlayerModal
              isEdit
              submitCallback={(playerDetails) => {
                return updatePlayerDetailsMutateAsync(playerDetails);
              }}
            />
          ) : null}
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};
export default ModeratorCard;
