import { QueryOptions, useApiQuery } from "./useApiQuery";
import { RoleActionDto } from "@/dto/RoleActionDto";

export const usePlayerRoleActions = (
  roomId: string,
  playerId: string,
  options?: QueryOptions<RoleActionDto[]>
) => {
  return useApiQuery<RoleActionDto[]>({
    queryKey: ["player-role-actions", roomId, playerId],
    query: {
      endpoint: `game/${roomId}/${playerId}/role-actions`,
    },
    options,
  });
};
