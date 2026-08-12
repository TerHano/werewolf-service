import { MyRoleDto } from "@/dto/MyRoleDto";
import { QueryOptions, useApiQuery } from "./useApiQuery";

export const myRoleQueryKey = "my-role";

/**
 * The caller's own card, including the player role id every action is addressed by. Null for
 * someone in the room who was never dealt in.
 */
export const useMyRole = (roomId: string, options?: QueryOptions<MyRoleDto>) => {
  return useApiQuery<MyRoleDto | null>({
    queryKey: [myRoleQueryKey, roomId],
    query: {
      endpoint: `game/${roomId}/my-role`,
    },
    options,
  });
};
