import { mutationOptions, useApiMutation } from "./useApiMutation";
import { VotePlayerOutDto } from "@/dto/VotePlayerOutDto";

export const useVotePlayerOut = (
  options: mutationOptions<void, VotePlayerOutDto> = {}
) => {
  return useApiMutation<void, VotePlayerOutDto>({
    mutation: {
      queryKeysToInvalidate: [["assigned-roles"]],
      endpoint: "game/vote-out-player",
      method: "POST",
    },
    ...options,
  });
};
