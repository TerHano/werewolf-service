import { useEndGame } from "@/hooks/useEndGame";
import { Stack } from "@chakra-ui/react";
import { Button } from "../../ui/button";
import { useRoomId } from "@/hooks/useRoomId";
import { useDayDetails } from "@/hooks/useDayDetails";
import { useSocketConnection } from "@/hooks/useSocketConnection";
import { NightCall } from "./NightView/NightCall";
import { useTranslation } from "react-i18next";
import {
  DrawerBackdrop,
  DrawerBody,
  DrawerCloseTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerRoot,
  DrawerTitle,
  DrawerTrigger,
} from "../../ui/drawer";
import { IconSettings } from "@tabler/icons-react";
import { Skeleton, SkeletonCircle } from "@/components/ui-addons/skeleton";
import { lazy, Suspense } from "react";
import { DrawerPlacementForMobileDesktop } from "@/util/drawer";

const ChoppingBlock = lazy(() => import("./DayView/ChoppingBlock"));

const DayNightVisual = lazy(
  () => import("@/components/GameRoom/ModeratorView/DayNightVisual")
);

export const ModeratorView = () => {
  const { t } = useTranslation();
  const roomId = useRoomId();

  const {
    data: dayDetails,
    refetch: refetchDayDetails,
    isLoading: isDayDetailsLoading,
  } = useDayDetails(roomId);

  const { mutate: endGameMutation, isPending: isEndingGame } = useEndGame();

  useSocketConnection({
    onDayOrTimeUpdated: () => {
      refetchDayDetails();
    },
  });

  const isDay = dayDetails?.isDay ?? false;

  return (
    <Skeleton width="100%" loading={isDayDetailsLoading}>
      <Stack>
        <Stack direction="row" justifyContent="space-between">
          <DrawerRoot placement={DrawerPlacementForMobileDesktop}>
            <DrawerBackdrop />
            <DrawerTrigger asChild>
              <Button size="sm" variant="subtle" colorPalette="blue">
                <IconSettings /> {t("game.settings.buttonLabel")}
              </Button>
            </DrawerTrigger>
            <DrawerContent>
              <DrawerCloseTrigger />
              <DrawerHeader>
                <DrawerTitle>{t("game.settings.title")}</DrawerTitle>
              </DrawerHeader>
              <DrawerBody mb={8}>
                <Button
                  w="full"
                  colorPalette="red"
                  loading={isEndingGame}
                  onClick={() => {
                    endGameMutation({ roomId });
                  }}
                >
                  {t("game.settings.endGame")}
                </Button>
              </DrawerBody>
            </DrawerContent>
          </DrawerRoot>
          <Suspense fallback={<SkeletonCircle size={10} loading />}>
            <DayNightVisual />
          </Suspense>
        </Stack>
        {isDay ? <ChoppingBlock /> : <NightCall />}
      </Stack>
    </Skeleton>
  );
};

export default ModeratorView;
