import { useDayDetails } from "@/hooks/useDayDetails";
import { useRoomId } from "@/hooks/useRoomId";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { useEffect, useMemo } from "react";
import dayImg from "@/assets/icons/game-room/cloudy-day.png";
import nightImg from "@/assets/icons/game-room/cloudy-night.png";
import { Badge, Box, Float, Image, Text } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { useAnimationReset } from "@/hooks/useAnimationReset";

export const DayNightVisual = () => {
  const roomId = useRoomId();
  const { t } = useTranslation();
  const { animation, resetAnimation } = useAnimationReset();
  const { data: dayDetails, refetch: refetchDayDetails } =
    useDayDetails(roomId);

  useSocketConnection({
    onDayOrTimeUpdated: () => {
      refetchDayDetails();
    },
  });

  const isDay = dayDetails?.isDay ?? false;

  const timeText = useMemo(() => {
    if (!dayDetails) {
      return t("game.actions.unknown");
    }
    if (dayDetails.currentNight === 0) {
      if (dayDetails.isDay) {
        return t("game.time.firstDayOrNight", {
          time: t("game.time.day"),
        });
      } else {
        return t("game.time.firstDayOrNight", {
          time: t("game.time.night"),
        });
      }
    } else {
      if (dayDetails.isDay) {
        return t(`Day ${dayDetails.currentNight}`);
      } else {
        return t(`Night ${dayDetails.currentNight}`);
      }
    }
  }, [dayDetails, t]);

  useEffect(() => {
    resetAnimation();
  }, [dayDetails, resetAnimation]);

  return (
    <Box
      className="animate-fade-in-and-rotate-from-right"
      animation={animation}
      mx={3}
      mb={5}
      position="relative"
      width="3rem"
    >
      <Image src={isDay ? dayImg : nightImg} />
      <Float placement="bottom-center">
        <Badge colorPalette={isDay ? "blue" : "purple"}>
          <Text>{timeText}</Text>
        </Badge>
      </Float>
    </Box>
  );
};

export default DayNightVisual;
