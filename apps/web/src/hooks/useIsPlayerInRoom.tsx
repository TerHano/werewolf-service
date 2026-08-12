import { QueryOptions, useApiQuery } from "./useApiQuery";

interface UseIsPlayerInRoomProps {
  roomId: string;
  options: QueryOptions<boolean>;
}

export const useIsPlayerInRoom = ({
  roomId,
  options,
}: UseIsPlayerInRoomProps) => {
  return useApiQuery<boolean>({
    queryKey: ["is-player-in-room", roomId],
    query: {
      endpoint: `room/${roomId}/is-player-in-room`,
    },
    options,
  });
};
