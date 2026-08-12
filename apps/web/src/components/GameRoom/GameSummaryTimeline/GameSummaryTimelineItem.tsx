import {
  TimelineConnector,
  TimelineContent,
  TimelineDescription,
  TimelineItem,
  TimelineTitle,
} from "@/components/ui/timeline";
import { GameActionDto } from "@/dto/GameActionDto";
import { useRoles } from "@/hooks/useRoles";
import { ActionType } from "@/enum/ActionType";
import { Role } from "@/enum/Role";
import { useRoleActionHelper } from "@/hooks/useRoleActionHelper";
import { useMemo } from "react";
import { useTranslation } from "react-i18next";
import { Image, Tokens } from "@chakra-ui/react";
import voteImg from "@/assets/icons/vote-board.png";

interface TimelineLabels {
  iconSrc?: string;
  iconOutline: Tokens["colors"];
  title?: string;
  description: string;
}

export const GameSummaryTimelineItem = ({
  action,
}: {
  action: GameActionDto;
}) => {
  const { t } = useTranslation();
  const { data: role } = useRoles();
  const { getActionButtonProps } = useRoleActionHelper();
  const roleDoingAction =
    action.action === ActionType.WerewolfKill
      ? role.find((r) => r.roleName === Role.WereWolf)
      : role.find((r) => r.roleName === action.player?.role);

  const affectedPlayerRole = role.find(
    (r) => r.roleName === action.affectedPlayer?.role
  );

  const timelineLabels = useMemo<TimelineLabels>(() => {
    switch (action.action) {
      case ActionType.VotedOut:
        return {
          iconOutline: "yellow.500",
          iconSrc: voteImg,
          title: t("Group Vote"),
          description: `The villagers chose to lynch ${action.affectedPlayer.nickname}`,
        };

      case ActionType.WerewolfKill:
        return {
          iconOutline: "red.500",
          iconSrc: roleDoingAction?.imgSrc,
          title: t("Werewolves"),
          description: `${getActionButtonProps(action.action).pastTenseLabel} ${action.affectedPlayer.nickname}`,
        };
      case ActionType.Revive:
        return {
          iconOutline: "green.500",
          iconSrc: roleDoingAction?.imgSrc,
          title: roleDoingAction?.label,
          description: `${getActionButtonProps(action.action).pastTenseLabel} ${action.affectedPlayer.nickname}`,
        };
      case ActionType.Investigate:
        return {
          iconOutline: "blue.500",
          iconSrc: roleDoingAction?.imgSrc,
          title: roleDoingAction?.label,
          description: `${getActionButtonProps(action.action).pastTenseLabel} ${action.affectedPlayer.nickname}`,
        };

      case ActionType.Suicide:
        return {
          iconOutline: "red.700",
          iconSrc: roleDoingAction?.imgSrc,
          title: roleDoingAction?.label,
          description: `${affectedPlayerRole?.label} decided to take their own life`,
        };

      case ActionType.VigilanteKill:
      case ActionType.Kill:
        return {
          iconOutline: "red.500",
          iconSrc: roleDoingAction?.imgSrc,
          title: roleDoingAction?.label,
          description: `${getActionButtonProps(action.action).pastTenseLabel} ${action.affectedPlayer.nickname}`,
        };

      default:
        return {
          iconOutline: "gray.500",
          title: t("Unkown"),
          description: `Unknown`,
        };
    }
  }, [
    action.action,
    action.affectedPlayer.nickname,
    affectedPlayerRole?.label,
    getActionButtonProps,
    roleDoingAction?.imgSrc,
    roleDoingAction?.label,
    t,
  ]);

  return (
    <TimelineItem>
      <TimelineConnector
        outlineWidth="1px"
        outlineOffset="1px"
        outlineStyle="solid"
        outlineColor={timelineLabels.iconOutline}
      >
        <Image src={timelineLabels.iconSrc} width="24px" />
      </TimelineConnector>
      <TimelineContent gap="1px">
        <TimelineTitle>{timelineLabels.title}</TimelineTitle>
        <TimelineDescription>{timelineLabels.description}</TimelineDescription>
        {/* <Text textStyle="sm">
          We shipped your product via <strong>FedEx</strong> and it should
          arrive within 3-5 business days.
        </Text> */}
      </TimelineContent>
    </TimelineItem>
  );
};
