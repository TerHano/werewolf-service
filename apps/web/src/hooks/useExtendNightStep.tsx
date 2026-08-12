import { RoomRequestDto } from "@/dto/RoomRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";
import { nightStateQueryKey } from "./useNightState";

/**
 * Gives the running night step more time. Badge holder only, and there is deliberately no
 * counterpart that shortens a step — that would make the length of a step a tell.
 */
export const useExtendNightStep = (
  options: mutationOptions<void, RoomRequestDto> = {}
) => {
  return useApiMutation<void, RoomRequestDto>({
    mutation: {
      queryKeysToInvalidate: [[nightStateQueryKey]],
      endpoint: "game/extend-step",
      method: "POST",
    },
    ...options,
  });
};
