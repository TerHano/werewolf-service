import { NightStateDto } from "@/dto/NightStateDto";
import { QueryOptions, useApiQuery } from "./useApiQuery";

export const nightStateQueryKey = "night-state";

export const useNightState = (
  roomId: string,
  options?: QueryOptions<NightStateDto>
) => {
  return useApiQuery<NightStateDto>({
    queryKey: [nightStateQueryKey, roomId],
    query: {
      endpoint: `game/${roomId}/night-state`,
    },
    options,
  });
};
