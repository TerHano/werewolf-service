import { Role } from "@/enum/Role";
import { RoleType } from "@/enum/RoleType";
import { useCallback, useMemo } from "react";
import moderatorImg from "@/assets/icons/roles/moderator-color.png";
import werewolfImg from "@/assets/icons/roles/werewolf-color.png";
import doctorImg from "@/assets/icons/roles/doctor-color.png";
import detectiveImg from "@/assets/icons/roles/seer-color.png";
import witchImg from "@/assets/icons/roles/witch-color.png";
import drunkImg from "@/assets/icons/roles/drunk-color.png";
import villagerImg from "@/assets/icons/roles/villager-color.png";
import vigilanteImg from "@/assets/icons/roles/vigilante-color.png";
import lycanImg from "@/assets/icons/roles/lycan-color.png";
import { useTranslation } from "react-i18next";

export interface RoleInfo {
  label: string;
  shortDescription: string;
  description: string;
  roleName: Role;
  roleType: RoleType;
  imgSrc: string;
  showInModeratorRoleCall: boolean;
  roleCallPriority: number;
}

export const useRoles = () => {
  const { t } = useTranslation();

  const data: RoleInfo[] = useMemo(
    () => [
      {
        label: t("roles.moderator.label"),
        shortDescription: t("roles.moderator.shortDescription"),
        description: t("roles.moderator.description"),
        roleName: Role.Moderator,
        roleType: RoleType.Moderator,
        roleCallPriority: 0,
        showInModeratorRoleCall: false,
        imgSrc: moderatorImg,
      },
      {
        label: t("roles.werewolf.label"),
        shortDescription: t("roles.werewolf.shortDescription"),
        description: t("roles.werewolf.description"),
        roleName: Role.WereWolf,
        roleType: RoleType.Enemy,
        imgSrc: werewolfImg,
        showInModeratorRoleCall: true,
        roleCallPriority: 1,
      },
      {
        label: t("roles.doctor.label"),
        shortDescription: t("roles.doctor.shortDescription"),
        description: t("roles.doctor.description"),
        roleName: Role.Doctor,
        roleType: RoleType.Traditional,
        imgSrc: doctorImg,
        showInModeratorRoleCall: true,
        roleCallPriority: 2,
      },
      {
        label: t("roles.detective.label"),
        shortDescription: t("roles.detective.shortDescription"),
        description: t("roles.detective.description"),
        roleName: Role.Detective,
        roleType: RoleType.Traditional,
        imgSrc: detectiveImg,
        showInModeratorRoleCall: true,
        roleCallPriority: 3,
      },
      {
        label: t("roles.witch.label"),
        shortDescription: t("roles.witch.shortDescription"),
        description: t("roles.witch.description"),
        roleName: Role.Witch,
        roleType: RoleType.Special,
        imgSrc: witchImg,
        showInModeratorRoleCall: true,
        roleCallPriority: 4,
      },
      {
        label: t("roles.drunk.label"),
        shortDescription: t("roles.drunk.shortDescription"),
        description: t("roles.drunk.description"),
        roleName: Role.Drunk,
        roleType: RoleType.Special,
        imgSrc: drunkImg,
        showInModeratorRoleCall: false,
        roleCallPriority: 5,
      },
      {
        label: t("roles.villager.label"),
        shortDescription: t("roles.villager.shortDescription"),
        description: t("roles.villager.description"),
        roleName: Role.Villager,
        roleType: RoleType.Villager,
        imgSrc: villagerImg,
        showInModeratorRoleCall: false,
        roleCallPriority: 6,
      },
      {
        label: t("roles.vigilante.label"),
        shortDescription: t("roles.vigilante.shortDescription"),
        description: t("roles.vigilante.description"),
        roleName: Role.Vigilante,
        roleType: RoleType.Special,
        imgSrc: vigilanteImg,
        showInModeratorRoleCall: true,
        roleCallPriority: 7,
      },
      {
        label: t("roles.cursed.label"),
        shortDescription: t("roles.cursed.shortDescription"),
        description: t("roles.cursed.description"),
        roleName: Role.Cursed,
        roleType: RoleType.Special,
        showInModeratorRoleCall: false,
        roleCallPriority: 8,
        imgSrc: lycanImg,
      },
    ],
    [t]
  );

  const isRoleType = useCallback(
    (role: number, type: RoleType) => {
      return data
        .filter((x) => x.roleType === type)
        .some((x) => x.roleName == role);
    },
    [data]
  );

  const getRoles = useCallback(
    (roles: number[]) => {
      if (!roles) return data;
      return data
        .sort((a, b) => a.roleCallPriority - b.roleCallPriority)
        .filter((role) => roles?.includes(role.roleName));
    },
    [data]
  );

  const getRole = useCallback(
    (roleId: number | undefined) => {
      const role = data.find((x) => x.roleName === roleId);
      return role;
    },
    [data]
  );

  // const getRoleForRoleId = (roleId: number) => {
  //   const role = data.find((x) => x.roleName === roleId);
  //   if (role === undefined) {
  //     throw new Error(`No Role For Id: ${roleId}`);
  //   }
  //   return role;
  // };

  return { getRole, getRoles, isRoleType, data };
};

export const getColorForRoleType = (roleType?: RoleType) => {
  switch (roleType) {
    case RoleType.Enemy:
      return "red";
    case RoleType.Special:
      return "green";
    case RoleType.Traditional:
      return "blue";
    default:
      return "lightgray";
  }
};
