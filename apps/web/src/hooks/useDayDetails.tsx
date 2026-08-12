import { useApiQuery } from "./useApiQuery";
import { DayDto } from "@/dto/DayDto";

export const useDayDetails = (roomId: string) => {
  return useApiQuery<DayDto>({
    queryKey: ["day-details", roomId],
    query: {
      endpoint: `game/${roomId}/day-time`,
    },
  });
};
