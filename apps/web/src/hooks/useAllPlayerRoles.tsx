import { useApiQuery } from "./useApiQuery";
import { PlayerRoleActionDto } from "@/dto/PlayerRoleActionDto";

export const useAllPlayerRolesQueryKey = "assigned-roles";

export const useAllPlayerRoles = (roomId: string) => {
  return useApiQuery<PlayerRoleActionDto[]>({
    queryKey: [useAllPlayerRolesQueryKey, roomId],
    query: {
      endpoint: `game/${roomId}/all-player-roles`,
    },
  });
};
