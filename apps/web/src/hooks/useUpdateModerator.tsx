import { UpdateModeratorDto } from "@/dto/UpdateModeratorDto";
import { mutationOptions, useApiMutation } from "./useApiMutation";

export const useUpdateModerator = (
  options: mutationOptions<void, UpdateModeratorDto> = {}
) => {
  return useApiMutation<void, UpdateModeratorDto>({
    mutation: {
      endpoint: "room/update-moderator",
      method: "POST",
    },
    ...options,
  });
};
