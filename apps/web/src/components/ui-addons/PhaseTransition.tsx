import { Box } from "@chakra-ui/react";
import { ReactNode, useEffect, useState } from "react";

/** Must match the leave animation in main.css, or the old screen is cut off mid-fade. */
const LEAVE_MS = 240;

interface PhaseTransitionProps<Phase> {
  /** A change here is what starts the swap. */
  phase: Phase;
  /** Rendered for whichever phase is currently on screen — not necessarily the latest one. */
  render: (phase: Phase) => ReactNode;
}

/**
 * Crossfades between two screens that replace each other.
 *
 * Dealing the cards swaps the lobby for the game in a single frame, which reads as a glitch
 * rather than as the game starting — the more so now that the first thing the game does is
 * deal a card with some ceremony. This holds the outgoing screen just long enough to fade it,
 * then brings the new one in.
 *
 * The phase is held rather than the rendered nodes: keeping React elements in state means
 * re-rendering stale ones, and the screen being faded out has live queries and a socket behind
 * it that should go on working right up until it goes.
 */
export const PhaseTransition = <Phase,>({
  phase,
  render,
}: PhaseTransitionProps<Phase>) => {
  const [shownPhase, setShownPhase] = useState(phase);
  const [isLeaving, setLeaving] = useState(false);

  useEffect(() => {
    if (phase === shownPhase) return;

    setLeaving(true);
    const timer = setTimeout(() => {
      setShownPhase(phase);
      setLeaving(false);
    }, LEAVE_MS);
    return () => clearTimeout(timer);
  }, [phase, shownPhase]);

  return (
    <Box
      w="100%"
      // Keyed on the phase so the entrance replays for each new screen rather than only once.
      key={String(shownPhase)}
      className={isLeaving ? "phase-leaving" : "phase-entering"}
    >
      {render(shownPhase)}
    </Box>
  );
};
