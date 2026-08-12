import { useLatestPlayerDeaths } from "@/hooks/useLatestPlayerDeaths";
import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { useRoomId } from "@/hooks/useRoomId";
import { Card, Group, Avatar, Badge, Text, Stack } from "@chakra-ui/react";
import { IconGrave2 } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";

export const KilledPlayersBanner = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  const { data: latestPlayerDeaths } = useLatestPlayerDeaths(roomId);
  const { getAvatarImageSrcForIndex } = usePlayerAvatar();

  if (!latestPlayerDeaths || latestPlayerDeaths.length === 0) {
    return (
      <Card.Root className="animate-fade-in-from-bottom" variant="outline">
        <Card.Body py={2}>
          <Card.Title>
            <Group gap={1} align="center">
              <IconGrave2 size={16} />
              <Text textStyle="accent">
                {t("game.choppingBlock.previousNightDeaths.title")}
              </Text>
            </Group>
          </Card.Title>
          <Card.Description>
            {t("game.choppingBlock.previousNightDeaths.noDeathsDescription")}
          </Card.Description>
        </Card.Body>
      </Card.Root>
    );
  }

  return (
    <Card.Root variant="outline">
      <Card.Body py={2}>
        <Card.Title>
          <Group gap={1} align="center">
            <IconGrave2 size={20} />
            <Text fontSize="xl" textStyle="accent">
              {t("game.choppingBlock.previousNightDeaths.title")}
            </Text>
          </Group>
        </Card.Title>
        <Card.Description>
          {t("game.choppingBlock.previousNightDeaths.description")}
          {/* {t("Once the sun had risen, the villagers found the following dead:")} */}
        </Card.Description>
        <Group mt="1rem">
          {latestPlayerDeaths.map((death) => (
            <Stack gap={1} align="center">
              <Avatar.Root
                borderRadius="xl"
                variant="subtle"
                size="md"
                key={death.id}
              >
                <Avatar.Image
                  marginTop={1}
                  src={getAvatarImageSrcForIndex(death.avatarIndex)}
                />
              </Avatar.Root>
              <Badge size="sm">{death.nickname}</Badge>
            </Stack>
          ))}
        </Group>
      </Card.Body>
    </Card.Root>
  );
};

export default KilledPlayersBanner;
