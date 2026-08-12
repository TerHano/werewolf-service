import { Role } from "@/enum/Role";
import { RoleInfo, useRoles } from "@/hooks/useRoles";
import { HStack, Box, Float, Badge } from "@chakra-ui/react";
import werewolfImg from "@/assets/icons/roles/werewolf-color.png";
import { useMemo } from "react";

interface RolesToShow {
  roles: RoleInfo[];
  remaining: number;
}
export const ActiveRolesList = ({
  widthOfContainer,
  numberOfWerewolves = 1,
  activeRoles = [],
}: {
  widthOfContainer: number | null;
  numberOfWerewolves?: number;
  activeRoles?: Role[];
}) => {
  const { getRoles } = useRoles();
  const data = getRoles(activeRoles);
  const rolesToShow = useMemo<RolesToShow>(() => {
    if (!widthOfContainer) {
      return { roles: data, remaining: 0 };
    }
    const widthOfContainerPlusWolfAndIndicator =
      widthOfContainer > 48 ? widthOfContainer - 48 : widthOfContainer;
    const toShow = Math.floor(widthOfContainerPlusWolfAndIndicator / 48);
    if (toShow + 1 < data.length) {
      return { roles: data.slice(0, toShow), remaining: data.length - toShow };
    }
    return { roles: data, remaining: 0 };
  }, [data, widthOfContainer]);

  return (
    <HStack>
      <Box position="relative" minW="32px" minH="32px" w="32px" h="32px">
        <img src={werewolfImg} alt="werewolf" />
        {numberOfWerewolves > 1 ? (
          <Float placement="top-end">
            <Badge variant="surface" size="xs">
              {
                // eslint-disable-next-line i18next/no-literal-string -- no need for translation
              }
              x{numberOfWerewolves}
            </Badge>
          </Float>
        ) : null}
      </Box>

      {rolesToShow.roles.map((role, index) => {
        return (
          <img
            key={`${role.roleName}-${index}`}
            src={role.imgSrc}
            alt={role.label}
            width="32"
            height="32"
          />
        );
      })}
      {rolesToShow.remaining > 0 ? (
        <Badge size="lg" variant="subtle" borderRadius="full">
          +{rolesToShow.remaining}
        </Badge>
      ) : null}
    </HStack>
  );
};
