import { getColorForRoleType, RoleInfo } from "@/hooks/useRoles";
import { Badge, Box, Image, Text, VStack } from "@chakra-ui/react";
import { IconMoon } from "@tabler/icons-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";

/**
 * The player's own card, dealt face down and turned over.
 *
 * The reveal is a one-shot: once it has played, the wrapper and the card back are dropped and
 * the role is left as plain content. Keeping a finished 3D transform around blurs text in some
 * browsers, and an invisible back face over the card is a trap waiting for a stray tap.
 */
export const PlayerRoleCard = ({ roleInfo }: { roleInfo: RoleInfo }) => {
  const { t } = useTranslation();
  const [isRevealed, setRevealed] = useState(false);

  const role = (
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

  if (isRevealed) {
    return role;
  }

  return (
    <Box className="deal-card">
      {/* The flip is the only animation on this element, so any animationend is that flip. */}
      <Box className="deal-card-inner" onAnimationEnd={() => setRevealed(true)}>
        <Box className="deal-card-face">{role}</Box>
        <Box
          className="deal-card-face deal-card-back"
          aria-hidden="true"
          display="flex"
          alignItems="center"
          justifyContent="center"
          borderRadius="xl"
          borderWidth="1px"
          borderColor="border.emphasized"
          bg="bg.emphasized"
          color="fg.muted"
          backgroundImage="repeating-linear-gradient(45deg, currentColor 0 1px, transparent 1px 12px)"
          // The pattern is a texture, not a picture — at full strength it fights the border.
          css={{ backgroundBlendMode: "overlay", opacity: 0.96 }}
        >
          <Box opacity={0.35}>
            <IconMoon size={56} />
          </Box>
        </Box>
      </Box>
    </Box>
  );
};
