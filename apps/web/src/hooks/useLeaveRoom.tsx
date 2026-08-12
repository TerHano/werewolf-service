import { mutationOptions, useApiMutation } from "./useApiMutation";
import { LeaveRoomRequestDto } from "@/dto/LeaveRoomRequestDto";

export const useLeaveRoom = (
  options: mutationOptions<void, LeaveRoomRequestDto> = {}
) => {
  return useApiMutation<void, LeaveRoomRequestDto>({
    mutation: {
      endpoint: "room/leave-room",
      method: "POST",
    },
    ...options,
  });
};
