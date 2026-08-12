import React from "react";

type RoomContextType = {
  roomId: string;
};
export const RoomContext = React.createContext<RoomContextType | undefined>(
  undefined
);
