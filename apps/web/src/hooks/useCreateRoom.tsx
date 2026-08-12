import { mutationOptions, useApiMutation } from "./useApiMutation";

export const useCreateRoom = (options: mutationOptions<string, void>) => {
  return useApiMutation<string, void>({
    mutation: {
      endpoint: "room/create-room",
      method: "POST",
    },
    ...options,
  });
};
