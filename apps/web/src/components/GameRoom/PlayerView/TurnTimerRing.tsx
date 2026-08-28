import { Box, Text } from "@chakra-ui/react";
import { useEffect, useRef, useState } from "react";

const SIZE = 84;
const STROKE = 6;
const RADIUS = (SIZE - STROKE) / 2;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

/** Below this the ring turns urgent — enough time to still act on the warning. */
const URGENT_SECONDS = 5;

/**
 * The clock on your own turn, as something to glance at rather than read.
 *
 * The step length is never sent to the client, and asking for it would be one more thing the
 * night has to keep quiet about. So the ring measures itself: the fullest the clock has been
 * seen this step is what full means. That also handles the badge holder granting more time —
 * the number jumps above anything seen before, and the ring simply refills.
 *
 * Shown only to the player whose turn it is, who already knows the step is theirs. Nothing
 * here is visible to anyone else, and no sound or vibration accompanies it: the length of a
 * step is exactly what the opaque night exists to hide from the room.
 */
export const TurnTimerRing = ({ secondsLeft }: { secondsLeft: number }) => {
  const [fullest, setFullest] = useState(secondsLeft);
  const deadlineRef = useRef(secondsLeft);

  useEffect(() => {
    // A rise can only mean the step was extended, or that this is the first reading.
    if (secondsLeft > deadlineRef.current) setFullest(secondsLeft);
    deadlineRef.current = secondsLeft;
  }, [secondsLeft]);

  const fraction = fullest > 0 ? Math.min(1, secondsLeft / fullest) : 0;
  const isUrgent = secondsLeft <= URGENT_SECONDS;

  return (
    <Box
      position="relative"
      width={`${SIZE}px`}
      height={`${SIZE}px`}
      alignSelf="center"
      color={isUrgent ? "red.400" : "blue.400"}
    >
      <svg
        width={SIZE}
        height={SIZE}
        viewBox={`0 0 ${SIZE} ${SIZE}`}
        aria-hidden="true"
      >
        <circle
          cx={SIZE / 2}
          cy={SIZE / 2}
          r={RADIUS}
          fill="none"
          stroke="currentColor"
          strokeWidth={STROKE}
          opacity={0.15}
        />
        <circle
          cx={SIZE / 2}
          cy={SIZE / 2}
          r={RADIUS}
          fill="none"
          stroke="currentColor"
          strokeWidth={STROKE}
          strokeLinecap="round"
          strokeDasharray={CIRCUMFERENCE}
          strokeDashoffset={CIRCUMFERENCE * (1 - fraction)}
          // Drawn from the top, and shortened rather than rotated, so it reads as time going.
          transform={`rotate(-90 ${SIZE / 2} ${SIZE / 2})`}
          style={{ transition: "stroke-dashoffset 250ms linear" }}
        />
      </svg>
      <Box
        position="absolute"
        inset="0"
        display="flex"
        alignItems="center"
        justifyContent="center"
      >
        <Text textStyle="accent" fontSize="2xl" lineHeight={1}>
          {secondsLeft}
        </Text>
      </Box>
    </Box>
  );
};
