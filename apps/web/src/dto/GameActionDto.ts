import { PlayerRoleDto } from "@/dto/PlayerRoleDto";
import { ActionType } from "@/enum/ActionType";

export interface GameActionDto {
  id: number;
  player?: PlayerRoleDto;
  action: ActionType;
  affectedPlayer: PlayerRoleDto;
}
