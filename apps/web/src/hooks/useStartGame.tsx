import { RoomRequestDto } from "@/dto/RoomRequestDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";
import { APIResponse } from "@/dto/APIResponse";

export const querysToInvalidateOnNewGame = [
  ["assigned-role"],
  ["day-details"],
  ["assigned-roles"],
  ["win-condition"],
  ["all-queued-actions"],
];

export const useStartGame = (
  options: mutationOptions<APIResponse, RoomRequestDto> = {}
) => {
  return useApiMutation<APIResponse, RoomRequestDto>({
    mutation: {
      endpoint: "room/start-game",
      method: "POST",
      queryKeysToInvalidate: querysToInvalidateOnNewGame,
    },
    ...options,
  });
};
