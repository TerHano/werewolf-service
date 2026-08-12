import { mutationOptions, useApiMutation } from "./useApiMutation";

export interface CheckRoomBody {
  roomId: string;
}

export const useCheckRoom = (
  options?: mutationOptions<boolean, CheckRoomBody>
) => {
  return useApiMutation<boolean, CheckRoomBody>({
    mutation: {
      endpoint: "room/check-room",
    },
    ...options,
  });
};
