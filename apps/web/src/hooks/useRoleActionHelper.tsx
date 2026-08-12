import { ActionType } from "@/enum/ActionType";
import { IconFirstAidKit, IconSword, IconZoom } from "@tabler/icons-react";
import { useCallback } from "react";
import { useTranslation } from "react-i18next";

interface ActionButton {
  label: string;
  pastTenseLabel: string;
  color: string;
  icon: React.ReactNode;
}

export const useRoleActionHelper = () => {
  const { t } = useTranslation();
  const getActionButtonProps = useCallback<
    (actionType: ActionType) => ActionButton
  >(
    (actionType: ActionType) => {
      switch (actionType) {
        case ActionType.VigilanteKill:
        case ActionType.WerewolfKill:
        case ActionType.Kill:
          return {
            label: t("game.actions.attack"),
            pastTenseLabel: t("game.actions.pastTense.kill"),
            color: "red",
            icon: <IconSword />,
          };
        case ActionType.Revive:
          return {
            label: t("game.actions.heal"),
            pastTenseLabel: t("game.actions.pastTense.heal"),
            color: "green",
            icon: <IconFirstAidKit />,
          };
        case ActionType.Investigate:
          return {
            label: t("game.actions.investigate"),
            pastTenseLabel: t("game.actions.pastTense.investigate"),
            color: "blue",
            icon: <IconZoom />,
          };
        default:
          return {
            label: t("game.actions.unknown"),
            pastTenseLabel: t("game.actions.unknown"),
            color: "white",
            icon: <IconSword />,
          };
      }
    },
    [t]
  );

  const getUndoActionText = useCallback(
    (actionType: ActionType) => {
      switch (actionType) {
        case ActionType.VigilanteKill:
        case ActionType.WerewolfKill:
        case ActionType.Kill:
          return t("game.actions.kill");
        case ActionType.Revive:
          return t("game.actions.heal");
        case ActionType.Investigate:
          return t("game.actions.investigate");
        default:
          return t("game.actions.unknown");
      }
    },
    [t]
  );

  return { getActionButtonProps, getUndoActionText };
};
