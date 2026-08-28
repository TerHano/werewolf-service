import { getColorForRoleType, RoleInfo } from "@/hooks/useRoles";
import { Badge, Box, Image, Text, VStack } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { IconChevronDown, IconMoon } from "@tabler/icons-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import { readFlag, writeFlag } from "@/util/preference";

/**
 * Whether to spell out what the role does. Kept per device rather than per game: someone who
 * knows how the Witch works knows it every night, and someone learning still has it in front
 * of them until they say otherwise.
 */
const ROLE_HELP_KEY = "werewolf.showRoleHelp";

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
  const [showHelp, setShowHelp] = useState(() => readFlag(ROLE_HELP_KEY, true));

  // Roles with no side of their own answer "lightgray", which is not a palette Chakra knows —
  // a tint from it is no tint at all. Those get a plain lift off the card instead.
  const rolePalette = getColorForRoleType(roleInfo?.roleType);
  const hasRoleTint = rolePalette !== "lightgray";

  const face = (
    <VStack
      className="deal-card-shape"
      justify="center"
      gap={1}
      px={3}
      py={4}
      borderWidth="1px"
      borderColor="border.emphasized"
      bg="bg.panel"
      boxShadow="lg"
    >
      {/* Sized to fill the card rather than float in the middle of it — the art is the card. */}
      <Image height="9.5rem" width="9.5rem" src={roleInfo?.imgSrc} />

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

      {/* The card and the slip beneath it are one object, so no gap may come between them. */}
      <VStack gap={0} w="100%">
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

      <VStack
        className={isRevealed ? undefined : "deal-card-detail"}
        gap={0}
        w="100%"
      >

        <Button
          variant="ghost"
          size="sm"
          color="fg.muted"
          mt={1}
          // Sits below the slip, so nothing comes between the card and the thing sliding out
          // from under it.
          order={1}
          aria-expanded={showHelp}
          aria-controls="role-help"
          onClick={() => {
            const next = !showHelp;
            setShowHelp(next);
            writeFlag(ROLE_HELP_KEY, next);
          }}
        >
          {showHelp ? t("game.role.hideHowItWorks") : t("game.role.howItWorks")}
          <Box
            as="span"
            display="inline-flex"
            className={`role-help-chevron ${showHelp ? "is-open" : ""}`}
          >
            <IconChevronDown size={14} />
          </Box>
        </Button>

        {/* Kept mounted whether open or not: a row that is removed has no height to animate. */}
        <Box
          className={`collapse-row role-sheet ${showHelp ? "is-open" : ""}`}
          aria-hidden={!showHelp}
        >
          <Box className="collapse-clip">
            {/*
              * Coloured by the role rather than left grey: it gives the slip an edge against
              * the card it slides out of, and the tint is the same one the role's name badge
              * already wears, so the two read as belonging to each other.
              */}
            <Box
              className="collapse-panel"
              colorPalette={hasRoleTint ? rolePalette : undefined}
              borderWidth="1px"
              borderTopWidth="0"
              borderColor={hasRoleTint ? "colorPalette.muted" : "border.emphasized"}
              borderBottomRadius="0.9rem"
              bg={hasRoleTint ? "colorPalette.subtle" : "bg.emphasized"}
              px={4}
              py={4}
            >
              <Text
                id="role-help"
                lineHeight="1.35em"
                textAlign="center"
                textStyle="accent"
                fontSize="md"
              >
                {roleInfo?.description}
              </Text>
            </Box>
          </Box>
        </Box>
      </VStack>
      </VStack>
    </VStack>
  );
};
