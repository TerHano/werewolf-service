import { NightStep } from "@/enum/NightStep";

/**
 * The night as this player may see it.
 *
 * Only the players acting in the running step get `currentStep` and `stepDeadline`; for
 * everyone else they are null. That is deliberate — if the room could see which step was
 * running, a step ending early would mean somebody acted and a step running its full length
 * would mean that role is dead.
 */
export interface NightStateDto {
  selfModerated: boolean;
  /** True while the server is walking the night. Everyone may know this. */
  isNightCallRunning: boolean;
  /** Non-null only when it is your turn — which is how you know it is. */
  currentStep: NightStep | null;
  /** UTC ISO string. Set only alongside currentStep. */
  stepDeadline: string | null;
  /** Whether you have already locked in for this step. */
  hasLockedIn: boolean;
  currentNight: number;
  isDay: boolean;
}
