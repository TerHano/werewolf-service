import { Role } from "@/enum/Role";

export interface PlayerRoleDto {
  id: string;
  nickname: string;
  role: Role;
}
