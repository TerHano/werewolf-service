import { NightStep } from "@/enum/NightStep";

export interface NightStateDto {
  selfModerated: boolean;
  /** Null when no night call is in progress — day, lobby, or waiting to begin. */
  currentStep: NightStep | null;
  /** UTC ISO string. Null whenever currentStep is null. */
  stepDeadline: string | null;
  currentNight: number;
  isDay: boolean;
  /** The room's full running order, fixed for the whole game. */
  steps: NightStep[];
}
