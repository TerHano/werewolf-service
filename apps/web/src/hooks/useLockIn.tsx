import { RoomRequestDto } from "@/dto/RoomRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";
import { nightStateQueryKey } from "./useNightState";

/**
 * Finishes your turn early. Once everyone acting in the step has locked in, the night moves on
 * without waiting out the clock.
 */
export const useLockIn = (options: mutationOptions<void, RoomRequestDto> = {}) => {
  return useApiMutation<void, RoomRequestDto>({
    mutation: {
      queryKeysToInvalidate: [[nightStateQueryKey]],
      endpoint: "game/lock-in",
      method: "POST",
    },
    ...options,
  });
};
