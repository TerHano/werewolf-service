import { RoomRequestDto } from "@/dto/RoomRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";

export const useEndGame = (
  options: mutationOptions<void, RoomRequestDto> = {}
) => {
  return useApiMutation<void, RoomRequestDto>({
    mutation: {
      endpoint: "room/end-game",
      method: "POST",
      queryKeysToInvalidate: [
        ["game-state"],
        ["win-condition"],
        ["assigned-role"],
        ["assigned-roles"],
        ["day-details"],
        ["all-queued-actions"],
        ["game-summary"],
      ],
    },
    ...options,
  });
};
