import { mutationOptions, useApiMutation } from "./useApiMutation";
import { KickPlayerDto } from "@/dto/KickPlayerDto";

export const useKickPlayer = (
  options: mutationOptions<void, KickPlayerDto> = {}
) => {
  return useApiMutation<void, KickPlayerDto>({
    mutation: {
      endpoint: "room/kick-player",
      method: "POST",
    },
    ...options,
  });
};
