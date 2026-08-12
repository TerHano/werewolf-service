import { Role } from "@/enum/Role";
import { RoleActionDto } from "./RoleActionDto";

export interface PlayerRoleActionDto {
  id: number;
  nickname: string;
  avatarIndex: number;
  role: Role;
  actions: RoleActionDto[];
  isAlive: boolean;
}
