import { useApiQuery } from "./useApiQuery";
import { Role } from "@/enum/Role";

export const useAssignedRole = (roomId: string) => {
  return useApiQuery<Role>({
    queryKey: ["assigned-role", roomId],
    query: {
      endpoint: `game/${roomId}/assigned-role`,
    },
  });
};
