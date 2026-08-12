import { useModerator } from "./useModerator";
import { useCurrentPlayer } from "./useCurrentPlayer";
import { useMemo } from "react";

export const useIsModerator = (roomId: string) => {
  const modQuery = useModerator(roomId);
  const currentPlayerQuery = useCurrentPlayer(roomId);

  const isModerator = useMemo(() => {
    if (!modQuery.data || !currentPlayerQuery.data) {
      return false;
    }
    return modQuery.data.id === currentPlayerQuery.data.id;
  }, [modQuery.data, currentPlayerQuery.data]);

  return {
    data: isModerator,
    isLoading: modQuery.isLoading || currentPlayerQuery.isLoading,
  };
};
