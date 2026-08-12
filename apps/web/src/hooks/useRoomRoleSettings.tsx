import { QueryOptions, useApiQuery } from "./useApiQuery";
import { RoomRoleSettingsDto } from "@/dto/RoomRoleSettingsDto";

export const useRoomRoleSettings = (
  roomId: string,
  options?: QueryOptions<RoomRoleSettingsDto>
) => {
  return useApiQuery<RoomRoleSettingsDto>({
    queryKey: ["room-role-settings", roomId],
    query: {
      endpoint: `room/${roomId}/role-settings`,
    },
    options,
  });
};
