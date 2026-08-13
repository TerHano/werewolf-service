import { Box, Card, Stack, Text } from "@chakra-ui/react";
import { IconMoon } from "@tabler/icons-react";

interface NightInProgressProps {
  title: string;
  description: string;
}

/**
 * What everyone who is not acting sees for the whole night.
 *
 * The animation exists only to show the app has not frozen — a card that never changes for a
 * minute or more reads as a crash. It is deliberately unconnected to game state: a continuous
 * CSS loop, started once, never restarted, never varied. Anything that ticked or reset when the
 * night moved on would hand back the per-step timing this screen exists to hide, which is the
 * whole reason players may end their turn early.
 *
 * The component also renders identically on every step, so React keeps the same DOM node and
 * the loop is not interrupted by the refetch that follows each `NightAdvanced`.
 */
export const NightInProgress = ({ title, description }: NightInProgressProps) => {
  return (
    <Card.Root className="animate-fade-in-from-bottom">
      <Card.Body>
        <Stack align="center" gap={4} py={6}>
          <Box className="animate-night-breathe" color="blue.300">
            <IconMoon size={56} />
          </Box>

          <Stack align="center" gap={2}>
            <Text textStyle="accent" fontSize="xl">
              {title}
            </Text>
            <Text color="dimmed" textAlign="center">
              {description}
            </Text>
          </Stack>

          {/* Staggered so it reads as a heartbeat rather than a progress indicator — there is
              deliberately nothing here that counts steps. */}
          <Stack direction="row" gap={2} aria-hidden>
            {[0, 1, 2].map((dot) => (
              <Box
                key={dot}
                className="animate-night-dot"
                style={{ animationDelay: `${dot * 0.3}s` }}
                w="6px"
                h="6px"
                borderRadius="full"
                bg="blue.300"
              />
            ))}
          </Stack>
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};
