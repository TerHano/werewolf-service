import { ActionType } from "@/enum/ActionType";

import { RoleActionDto } from "@/dto/RoleActionDto";
import { lazy } from "react";

const SimpleActionModal = lazy(() => import("./SimpleActionModal"));

const InvestigationModal = lazy(
  () => import("../InvestigationModals/InvestigationModal")
);

export interface ActionModalBtnTriggerProps {
  action: RoleActionDto;
  playerRoleId?: number;
}

export const ActionModalBtnTrigger = ({
  action,
  playerRoleId,
}: ActionModalBtnTriggerProps) => {
  switch (action.type) {
    case ActionType.Investigate:
      if (!playerRoleId) return null;
      return <InvestigationModal playerRoleId={playerRoleId} action={action} />;
    default:
      return <SimpleActionModal playerRoleId={playerRoleId} action={action} />;
  }
};
