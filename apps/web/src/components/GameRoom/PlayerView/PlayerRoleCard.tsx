import { getColorForRoleType, RoleInfo } from "@/hooks/useRoles";
import { Badge, Box, Image, Text, VStack } from "@chakra-ui/react";
import { IconMoon } from "@tabler/icons-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";

/**
 * The player's own card, dealt face down and turned over.
 *
 * The card is a card: 5:7 like the ones on the table, carrying the art and the name and
 * nothing else. What a role may actually do is a rule of the game rather than something
 * printed on the card, so it sits below and arrives once the card has been turned.
 *
 * The reveal is a one-shot. Once it has played the 3D wrapper and the card back are dropped
 * and the card is left as ordinary content: a finished rotate3d blurs text in some browsers,
 * and an invisible back face lying over the card is a trap waiting for a stray tap.
 */
export const PlayerRoleCard = ({ roleInfo }: { roleInfo: RoleInfo }) => {
  const { t } = useTranslation();
  const [isRevealed, setRevealed] = useState(false);

  const face = (
    <VStack
      className="deal-card-shape"
      justify="center"
      gap={2}
      px={4}
      borderWidth="1px"
      borderColor="border.emphasized"
      bg="bg.panel"
      boxShadow="lg"
    >
      <Image height="7rem" width="7rem" src={roleInfo?.imgSrc} />

      <Badge
        variant="subtle"
        colorPalette={getColorForRoleType(roleInfo?.roleType)}
      >
        <Text lineHeight={1} fontSize="2xl" textStyle="accent">
          {roleInfo?.label ?? "ROLE_LABEL"}
        </Text>
      </Badge>

      <Text
        lineHeight={1.1}
        color="gray.400"
        fontSize="md"
        textAlign="center"
        textStyle="accent"
      >
        {roleInfo?.shortDescription ?? "Lorem Ipsum Lorem Ipsum"}
      </Text>
    </VStack>
  );

  return (
    <VStack mt={6} gap={6}>
      <Text textStyle="accent" fontSize="xl">
        {t("Your Role Is...")}
      </Text>

      {isRevealed ? (
        face
      ) : (
        <Box className="deal-card">
          {/* The flip is the only animation here, so any animationend is that flip. */}
          <Box
            className="deal-card-inner deal-card-shape"
            onAnimationEnd={() => setRevealed(true)}
          >
            <Box className="deal-card-face">{face}</Box>
            <Box
              className="deal-card-face deal-card-back"
              aria-hidden="true"
              display="flex"
              alignItems="center"
              justifyContent="center"
              borderWidth="1px"
              borderColor="border.emphasized"
              bg="bg.emphasized"
              color="fg.muted"
              boxShadow="lg"
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
      )}

      <Text
        className={isRevealed ? undefined : "deal-card-detail"}
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
