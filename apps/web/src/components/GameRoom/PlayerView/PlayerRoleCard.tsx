import { getColorForRoleType, RoleInfo } from "@/hooks/useRoles";
import { Badge, Image, Text, VStack } from "@chakra-ui/react";
import { useTranslation } from "react-i18next";

export const PlayerRoleCard = ({ roleInfo }: { roleInfo: RoleInfo }) => {
  const { t } = useTranslation();
  return (
    <VStack mt={6} gap={6}>
      <Text textStyle="accent" fontSize="xl">
        {t("Your Role Is...")}
      </Text>
      <VStack gap={1}>
        <Image height="8rem" width="8rem" src={roleInfo?.imgSrc} />

        <Badge
          variant="subtle"
          colorPalette={getColorForRoleType(roleInfo?.roleType)}
        >
          <Text lineHeight={1} fontSize="2xl" textStyle="accent">
            {roleInfo?.label ?? "ROLE_LABEL"}
          </Text>
        </Badge>

        <Text lineHeight={1} color="gray.400" fontSize="lg" textStyle="accent">
          {roleInfo?.shortDescription ?? "Lorem Ipsum Lorem Ipsum"}
        </Text>
      </VStack>

      <Text
        lineHeight="1.2em"
        textAlign="center"
        textStyle="accent"
        fontSize="lg"
      >
        {roleInfo?.description}
      </Text>
    </VStack>
  );
};
