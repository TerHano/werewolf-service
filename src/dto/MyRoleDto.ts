import { Role } from "@/enum/Role";

export interface MyRoleDto {
  /** The id every game action is addressed by — not the same as the player_room id. */
  playerRoleId: number;
  role: Role;
  isAlive: boolean;
  nightKilled: number;
}
