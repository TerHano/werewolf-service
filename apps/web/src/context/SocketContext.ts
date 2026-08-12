import React from "react";

export const SocketContext = React.createContext<signalR.HubConnection | null>(
  null
);
