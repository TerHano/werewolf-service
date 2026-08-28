import { Badge, Box, Stack, Text } from "@chakra-ui/react";
import { IconEye, IconMoon, IconSun } from "@tabler/icons-react";
import { useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { GamePlayerDto } from "@/dto/GamePlayerDto";
import { MyRoleDto } from "@/dto/MyRoleDto";
import { getColorForRoleType, useRoles } from "@/hooks/useRoles";

/** Long enough to read, short enough that a card left face up is your own doing. */
const REVEAL_MS = 5000;

interface GameStatusBarProps {
  currentNight: number;
  isDay: boolean;
  players: GamePlayerDto[];
  myRole: MyRoleDto | null;
}

/**
 * Where the game is, in one line.
 *
 * Everything here is already public: which night it is, and how many are still in — deaths are
 * announced to the whole table each morning, so counting them is not a leak. It exists because
 * the game otherwise gives a player who picks their phone up mid-round nothing to orient by.
 *
 * Your own role is the exception, and is why it is behind a tap and hides itself again after a
 * few seconds. It is your card, so no rule stops you looking at it — but a phone lying face up
 * on a table is read by whoever is sitting next to you, and a role that stayed on screen would
 * be a tell rather than a reminder.
 */
export const GameStatusBar = ({
  currentNight,
  isDay,
  players,
  myRole,
}: GameStatusBarProps) => {
  const { t } = useTranslation();
  const { getRole } = useRoles();
  const [isRoleShowing, setRoleShowing] = useState(false);

  useEffect(() => {
    if (!isRoleShowing) return;
    const timer = setTimeout(() => setRoleShowing(false), REVEAL_MS);
    return () => clearTimeout(timer);
  }, [isRoleShowing]);

  const timeLabel = isDay ? t("game.time.day") : t("game.time.night");
  const phase =
    currentNight === 0
      ? t("game.time.firstDayOrNight", { time: timeLabel })
      : `${timeLabel} ${currentNight + 1}`;

  const aliveCount = players.filter((player) => player.isAlive).length;
  const roleInfo = myRole ? getRole(myRole.role) : undefined;

  return (
    <Stack
      direction="row"
      align="center"
      justify="space-between"
      gap={2}
      px={1}
    >
      <Stack direction="row" align="center" gap={2} color="fg.muted">
        {isDay ? <IconSun size={16} /> : <IconMoon size={16} />}
        <Text textStyle="accent" fontSize="sm">
          {phase}
        </Text>
        <Text fontSize="sm" color="dimmed">
          {t("game.status.alive", { count: aliveCount })}
        </Text>
      </Stack>

      {roleInfo && (
        <Badge
          as="button"
          type="button"
          size="sm"
          variant={isRoleShowing ? "subtle" : "outline"}
          colorPalette={
            isRoleShowing ? getColorForRoleType(roleInfo.roleType) : undefined
          }
          cursor="pointer"
          aria-live="polite"
          aria-label={t("game.status.peek")}
          onClick={() => setRoleShowing((showing) => !showing)}
        >
          <Box as="span" display="inline-flex" mr={1}>
            <IconEye size={12} />
          </Box>
          {isRoleShowing ? roleInfo.label : t("game.status.peek")}
        </Badge>
      )}
    </Stack>
  );
};
