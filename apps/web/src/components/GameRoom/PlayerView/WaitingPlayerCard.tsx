import { Image, Text, Stack } from "@chakra-ui/react";
import waitingRoomImg from "@/assets/icons/game-room/waiting-room.png";
import { useTranslation } from "react-i18next";
import { LeaveRoomButton } from "@/components/Lobby/LeaveRoomButton";

export const WaitingPlayerCard = () => {
  const { t } = useTranslation();
  return (
    <Stack gap={2}>
      <Image alignSelf="center" width="10rem" src={waitingRoomImg} />
      <Stack gap={0}>
        <Text fontSize="xl">{t("Game In Progress")}</Text>
        <Text color="gray.400" fontSize="sm">
          {t("You will join the next game once the current one is finished")}
        </Text>
        <LeaveRoomButton />
      </Stack>
    </Stack>
  );
};
