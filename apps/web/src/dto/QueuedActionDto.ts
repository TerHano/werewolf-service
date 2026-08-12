import { ActionType } from "@/enum/ActionType";

export interface QueuedActionDto {
  id: number;
  playerRoleId?: number;
  action: ActionType;
  affectedPlayerRoleId: number;
}
