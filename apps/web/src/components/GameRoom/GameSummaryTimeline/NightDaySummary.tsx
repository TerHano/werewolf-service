import { GameSummaryTimelineItem } from "@/components/GameRoom/GameSummaryTimeline/GameSummaryTimelineItem";
import { GameActionDto } from "@/dto/GameActionDto";
import { Badge, Stack, Text, TimelineRoot } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";

export const NightDaySummary = ({
  night,
  actions,
  isDay,
}: {
  night: number;
  actions: GameActionDto[];
  isDay?: boolean;
}) => {
  const { t } = useTranslation();
  const dayNightLabel = isDay ? t("game.time.day") : t("game.time.night");
  const timelineHeader =
    night === 0
      ? t(`game.time.firstDayOrNight`, {
          time: dayNightLabel,
        })
      : `${dayNightLabel} ${night + 1}`;
  return (
    <Stack gap={3}>
      <Badge w="fit-content" colorPalette={isDay ? "blue" : "purple"}>
        {timelineHeader}
      </Badge>
      <TimelineRoot ml={6} size="xl" variant="subtle">
        {actions.length === 0 ? (
          isDay ? (
            <DayNoActionsTaken />
          ) : (
            <NightNoActionsTaken />
          )
        ) : (
          actions.map((a) => <GameSummaryTimelineItem action={a} />)
        )}
      </TimelineRoot>
    </Stack>
  );
};

const DayNoActionsTaken = () => {
  const { t } = useTranslation();

  return (
    <Text my={1} fontStyle="italic" fontSize="lg" textStyle="accent">
      {t("gameSummary.noEventsForDay")}
    </Text>
  );
};

const NightNoActionsTaken = () => {
  const { t } = useTranslation();
  return (
    <Text my={1} fontStyle="italic" fontSize="lg" textStyle="accent">
      {t("gameSummary.noEventsForNight")}
    </Text>
  );
};
