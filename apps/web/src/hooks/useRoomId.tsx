import { RoomContext } from "@/context/RoomProvider";
import React from "react";

export const useRoomId = () => {
  const context = React.useContext(RoomContext);
  if (context === undefined) {
    throw new Error("useRoomId must be used within a RoomProvider");
  }
  return context.roomId;
};
