import werewolfImg from "@/assets/icons/roles/werewolf-color.png";
import villagerImg from "@/assets/icons/roles/villager-color.png";
import { VStack, Image, Text, Highlight } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";
import { InvestigatePlayerResponse } from "@/hooks/useInvestigatePlayer";

export const WerewolfInvestigationResult = ({
  investigationResult,
}: {
  investigationResult: InvestigatePlayerResponse | null;
}) => {
  const { t } = useTranslation();
  const suspect = investigationResult?.playerRole;
  const isWerewolf = investigationResult?.isInvestigationSuccessful;

  if (isWerewolf) {
    return (
      <VStack className="animate-fade-in-from-bottom">
        <Image src={werewolfImg} width="10rem" alt="Werewolf" />
        <Text fontSize="md">
          <Highlight
            styles={{ bg: "green.subtle", color: "green.fg" }}
            query={t("IS")}
          >
            {t(`YIKES! ${suspect?.nickname} IS a Werewolf!`)}
          </Highlight>
        </Text>
      </VStack>
    );
  } else {
    return (
      <VStack className="animate-fade-in-from-bottom">
        <Image src={villagerImg} width="10rem" alt="Villager" />
        <Text fontSize="md">
          <Highlight
            styles={{ bg: "red.subtle", color: "red.fg" }}
            query={t("NOT")}
          >
            {t(`${suspect?.nickname} is NOT a Werewolf!`)}
          </Highlight>
        </Text>
      </VStack>
    );
  }
};

export default WerewolfInvestigationResult;
