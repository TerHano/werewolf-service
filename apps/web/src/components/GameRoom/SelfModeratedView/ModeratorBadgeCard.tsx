import { Box, Card, Stack, Text } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import {
  IconChevronDown,
  IconClockPlus,
  IconMicrophone,
  IconFlag,
} from "@tabler/icons-react";
import { useTranslation } from "react-i18next";
import { useRoomId } from "@/hooks/useRoomId";
import { useExtendNightStep } from "@/hooks/useExtendNightStep";
import { useEndGame } from "@/hooks/useEndGame";
import { readFlag, writeFlag } from "@/util/preference";
import { useState } from "react";
import {
  DialogActionTrigger,
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogRoot,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";

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
 *
 * Ending the game does live here, because somebody has to be able to call it when the table
 * gives up on a night, and the badge holder is the one person the room already treats as
 * running it. It asks first: it ends the game for everyone, and there is no resuming.
 *
 * The job description folds away, and stays folded. Spelled out it ran to half a phone screen,
 * which pushed the holder's own card below the fold — the one person who has to look at a card
 * *and* run the table had the most trouble seeing theirs. It is read once; the card is looked
 * at every night.
 */
const BADGE_HELP_KEY = "werewolf.showBadgeHelp";

export const ModeratorBadgeCard = ({
  isNightCallRunning,
  onExtended,
}: ModeratorBadgeCardProps) => {
  const { t } = useTranslation();
  const roomId = useRoomId();
  // Folded by default, unlike the role's own description: the title already says the thing
  // that matters, and the rest is a job description that only the first-time holder needs.
  // Unfolding is one tap, and the choice sticks.
  const [showHelp, setShowHelp] = useState(() => readFlag(BADGE_HELP_KEY, false));

  const { mutate: extendStep, isPending: isExtending } = useExtendNightStep({
    onSuccess: async () => onExtended(),
  });

  // Everyone's client is told the game state changed, so no local refresh is needed here.
  const { mutate: endGame, isPending: isEndingGame } = useEndGame();

  return (
    <Card.Root variant="subtle">
      <Card.Body>
        <Stack gap={3}>
          <Stack
            direction="row"
            align="center"
            gap={3}
            cursor="pointer"
            role="button"
            tabIndex={0}
            aria-expanded={showHelp}
            aria-controls="badge-help"
            onClick={() => {
              const next = !showHelp;
              setShowHelp(next);
              writeFlag(BADGE_HELP_KEY, next);
            }}
            onKeyDown={(event) => {
              if (event.key !== "Enter" && event.key !== " ") return;
              event.preventDefault();
              const next = !showHelp;
              setShowHelp(next);
              writeFlag(BADGE_HELP_KEY, next);
            }}
          >
            <IconMicrophone />
            <Text textStyle="accent" flex="1">
              {t("game.badge.title")}
            </Text>
            <Box
              as="span"
              display="inline-flex"
              color="fg.muted"
              className={`role-help-chevron ${showHelp ? "is-open" : ""}`}
            >
              <IconChevronDown size={16} />
            </Box>
          </Stack>

          {/* Kept mounted so it has a height to animate away to. */}
          <Box
            className={`collapse-row ${showHelp ? "is-open" : ""}`}
            aria-hidden={!showHelp}
            w="100%"
            mt={-3}
          >
            <Box className="collapse-clip">
              <Box className="collapse-panel" pt={3}>
                <Text id="badge-help" color="dimmed" fontSize="sm">
                  {t("game.badge.description")}
                </Text>
              </Box>
            </Box>
          </Box>

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

          <DialogRoot role="alertdialog">
            <DialogTrigger asChild>
              <Button size="sm" variant="ghost" colorPalette="red">
                <IconFlag size={16} />
                {t("game.badge.endGame.button")}
              </Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>
                  <Text textStyle="accent" fontSize="xl">
                    {t("game.badge.endGame.title")}
                  </Text>
                </DialogTitle>
              </DialogHeader>
              <DialogBody>
                <Text fontSize="sm">{t("game.badge.endGame.description")}</Text>
              </DialogBody>
              <DialogFooter>
                <DialogActionTrigger asChild>
                  <Button variant="outline">
                    {t("game.badge.endGame.cancel")}
                  </Button>
                </DialogActionTrigger>
                <Button
                  colorPalette="red"
                  loading={isEndingGame}
                  disabled={isEndingGame}
                  onClick={() => endGame({ roomId })}
                >
                  {t("game.badge.endGame.confirm")}
                </Button>
              </DialogFooter>
            </DialogContent>
          </DialogRoot>
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};
