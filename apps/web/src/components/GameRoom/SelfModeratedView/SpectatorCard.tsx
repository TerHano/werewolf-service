import { Badge, Card, SimpleGrid, Stack, Text } from "@chakra-ui/react";
import { Avatar } from "@/components/ui/avatar";
import { useTranslation } from "react-i18next";
import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { MyRoleDto } from "@/dto/MyRoleDto";
import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { useRoles } from "@/hooks/useRoles";

interface SpectatorCardProps {
  myRole: MyRoleDto;
  players: GamePlayerDto[];
}

/**
 * What a dead player sees.
 *
 * They get the same public picture as everyone else — who is alive, who is out — plus their own
 * card, which they already knew. Deliberately **not** the other players' roles: a dead player
 * is still sitting at the table with a face, and one who knows the whole cast gives it away by
 * reacting. The full reveal stays where it has always been, on the end-of-game summary.
 */
export const SpectatorCard = ({ myRole, players }: SpectatorCardProps) => {
  const { t } = useTranslation();
  const { getAvatarImageSrcForIndex } = usePlayerAvatar();
  const { getRole } = useRoles();

  const roleInfo = getRole(myRole.role);
  const alive = players.filter((player) => player.isAlive);
  const dead = players.filter((player) => !player.isAlive);

  return (
    <Card.Root className="animate-fade-in-from-bottom">
      <Card.Body>
        <Stack gap={5}>
          <Stack gap={1} align="center">
            <Text textStyle="accent" fontSize="lg">
              {t("game.spectator.title")}
            </Text>
            <Text color="dimmed" fontSize="sm" textAlign="center">
              {t("game.spectator.description")}
            </Text>
            {roleInfo && (
              <Badge variant="subtle">
                {t("game.spectator.youWere", { role: roleInfo.label })}
              </Badge>
            )}
          </Stack>

          <Stack gap={2}>
            <Text textStyle="accent" fontSize="sm">
              {t("game.spectator.stillAlive", { count: alive.length })}
            </Text>
            <SimpleGrid columns={{ base: 3, sm: 5 }} gap={3}>
              {alive.map((player) => (
                <Stack key={player.id} gap={0} align="center">
                  <Avatar
                    size="lg"
                    shape="rounded"
                    src={getAvatarImageSrcForIndex(player.avatarIndex)}
                  />
                  <Badge>{player.nickname}</Badge>
                </Stack>
              ))}
            </SimpleGrid>
          </Stack>

          {dead.length > 0 && (
            <Stack gap={2}>
              <Text textStyle="accent" fontSize="sm">
                {t("game.spectator.out", { count: dead.length })}
              </Text>
              <SimpleGrid columns={{ base: 3, sm: 5 }} gap={3}>
                {dead.map((player) => (
                  <Stack key={player.id} gap={0} align="center" opacity={0.45}>
                    <Avatar
                      size="lg"
                      shape="rounded"
                      src={getAvatarImageSrcForIndex(player.avatarIndex)}
                    />
                    <Badge>{player.nickname}</Badge>
                  </Stack>
                ))}
              </SimpleGrid>
            </Stack>
          )}
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};
