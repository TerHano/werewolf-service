import { mutationOptions, useApiMutation } from "./useApiMutation";
import { AddEditPlayerDetailsDto } from "@/dto/AddEditPlayerDetailsDto";

export const useUpdateCurrentPlayerDetails = (
  options?: mutationOptions<void, AddEditPlayerDetailsDto>
) => {
  return useApiMutation<void, AddEditPlayerDetailsDto>({
    mutation: {
      endpoint: `player/update-player`,
      method: "POST",
      queryKeysToInvalidate: [
        [`players`],
        ["moderator"],
        ["assigned-roles"],
        ["current-player"],
      ],
    },
    ...options,
  });
};
