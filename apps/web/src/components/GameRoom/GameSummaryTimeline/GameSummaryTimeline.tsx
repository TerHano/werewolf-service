import { useRoomId } from "@/hooks/useRoomId";
import { useGameSummary } from "@/hooks/useGameSummary";
import { Separator, Skeleton, Stack, Text } from "@chakra-ui/react";
import { NightDaySummary } from "@/components/GameRoom/GameSummaryTimeline/NightDaySummary";
import { useTranslation } from "react-i18next";

export const GameSummaryTimeline = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();

  const { data: gameSummary, isLoading: isGameSummaryLoading } =
    useGameSummary(roomId);

  if (isGameSummaryLoading) {
    return <Skeleton width="100%" height={100} />;
  }

  if (!gameSummary || gameSummary.length === 0) {
    return (
      <Stack mt={4} gap={3} justify="start" w="full">
        <Text fontSize="md">{t("gameSummary.title")}</Text>
        <Text fontSize="sm" color="gray.500">
          {t("gameSummary.noEvents")}
        </Text>
      </Stack>
    );
  }

  return (
    <Stack mt={4} gap={3} justify="start" w="full">
      <Text fontSize="md">{t("gameSummary.title")}</Text>
      {gameSummary?.map((summary) => {
        return (
          <>
            <Stack gap={2}>
              <NightDaySummary
                night={summary.night}
                actions={summary.nightActions}
              />
              <NightDaySummary
                isDay
                night={summary.night}
                actions={summary.dayActions}
              />
              <Separator flex="1" />
            </Stack>
          </>
        );
      })}
    </Stack>
  );

  //   {gameSummary?.map((summary) =>
  //     <TimelineRoot>
  //    {sumamry.nightActions.map((a) => {
  //       return <GameSummaryTimelineItem action={a} />;
  //     })}</TimelineRoot>
  //   )}
  // </TimelineRoot>
};

export default GameSummaryTimeline;
