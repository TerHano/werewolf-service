import { RoomRequestDto } from "@/dto/RoomRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";

export const useEndNight = (
  options: mutationOptions<void, RoomRequestDto> = {}
) => {
  return useApiMutation<void, RoomRequestDto>({
    mutation: {
      queryKeysToInvalidate: [["assigned-roles"]],
      endpoint: "game/end-night",
      method: "POST",
    },
    ...options,
  });
};
