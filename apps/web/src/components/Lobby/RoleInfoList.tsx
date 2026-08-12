import { RoleInfo } from "@/hooks/useRoles";
import { Separator, HStack, VStack, Stack, Text } from "@chakra-ui/react";
import React from "react";

export const RoleInfoList = ({ roles }: { roles: RoleInfo[] }) => {
  return (
    <Stack gap={3}>
      {roles.map((role, index) => (
        <React.Fragment key={role.roleName}>
          {index > 0 ? <Separator /> : null}

          <HStack gap={5}>
            <VStack minW="4rem" gap={1}>
              <img width={36} src={role.imgSrc} alt={role.label} />
              <Text textStyle="accent" fontSize={18}>
                {role.label}
              </Text>
            </VStack>
            <Stack>
              <Text
                textStyle="accent"
                color="gray.400"
                fontStyle="italic"
                fontWeight={600}
                fontSize="1.2rem"
              >
                {role.shortDescription}
              </Text>
              <Text>{role.description}</Text>
            </Stack>
          </HStack>
        </React.Fragment>
      ))}
    </Stack>
  );
};

export default RoleInfoList;
