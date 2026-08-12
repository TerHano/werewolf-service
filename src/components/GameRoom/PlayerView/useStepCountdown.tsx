import { useEffect, useState } from "react";

/**
 * Seconds left on the current night step, ticking down locally.
 *
 * The deadline is authoritative and comes from the server, so a client whose clock has drifted
 * or which reconnected mid-step still lands on the right number — we only interpolate between
 * server updates rather than counting from a local start time.
 */
export const useStepCountdown = (deadline: string | null | undefined) => {
  const [secondsLeft, setSecondsLeft] = useState<number | null>(null);

  useEffect(() => {
    if (!deadline) {
      setSecondsLeft(null);
      return;
    }

    const endsAt = new Date(deadline).getTime();
    const tick = () =>
      setSecondsLeft(Math.max(0, Math.ceil((endsAt - Date.now()) / 1000)));

    tick();
    const interval = setInterval(tick, 250);
    return () => clearInterval(interval);
  }, [deadline]);

  return secondsLeft;
};
