import { ActionType } from "@/enum/ActionType";

export interface QueuedActionRequestDto {
  roomId: string;
  playerRoleId?: number;
  action: ActionType;
  affectedPlayerRoleId: number;
}
