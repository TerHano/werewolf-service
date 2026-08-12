import { ActionType } from "@/enum/ActionType";

export interface RoleActionDto {
  label: string;
  type: ActionType;
  enabled: boolean;
  disabledReason?: string;
  validPlayerIds: number[];
}
