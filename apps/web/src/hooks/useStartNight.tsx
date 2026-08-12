import { RoomRequestDto } from "@/dto/RoomRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";
import { nightStateQueryKey } from "./useNightState";

export const useStartNight = (
  options: mutationOptions<void, RoomRequestDto> = {}
) => {
  return useApiMutation<void, RoomRequestDto>({
    mutation: {
      queryKeysToInvalidate: [[nightStateQueryKey]],
      endpoint: "game/start-night",
      method: "POST",
    },
    ...options,
  });
};
