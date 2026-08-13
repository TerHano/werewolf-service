import {
  CheckboxGroup,
  Field as ChakraField,
  Float,
  Group,
  Image,
  Separator,
  SimpleGrid,
  Stack,
  Tabs,
  Text,
  DrawerFooter,
  HStack,
  useDrawer,
  DrawerRootProvider,
} from "@chakra-ui/react";
import werewolfImg from "@/assets/icons/roles/werewolf-color.png";
import { CheckboxCard, CheckboxCardIndicator } from "../../ui/checkbox-card";
import { useRoles } from "@/hooks/useRoles";
import { RoleType } from "@/enum/RoleType";
import { Controller, SubmitHandler, useForm } from "react-hook-form";
import { useTranslation } from "react-i18next";
import { useCallback } from "react";
import { useRoomRoleSettings } from "@/hooks/useRoomRoleSettings";
import { useRoomId } from "@/hooks/useRoomId";
import { RoomRoleSettingsDto } from "@/dto/RoomRoleSettingsDto";
import { useUpdateRoomRoleSettings } from "@/hooks/useUpdateRoomRoleSettings";
import { Button } from "../../ui/button";
import { SegmentedControl } from "../../ui/segmented-control";
import { RoleInformationDialog } from "@/components/Lobby/RoleInformationDialog";
import { Switch } from "@/components/ui/switch";
import { Field } from "@/components/ui/field";
import {
  DrawerBackdrop,
  DrawerBody,
  DrawerCloseTrigger,
  DrawerContent,
  DrawerHeader,
  DrawerTrigger,
} from "@/components/ui/drawer";
import { IconCards, IconSettings } from "@tabler/icons-react";
import { Skeleton } from "@/components/ui-addons/skeleton";
import { useToaster } from "@/hooks/ui/useToaster";
import { DrawerPlacementForMobileDesktop } from "@/util/drawer";

interface EditRoomRoleSettingsForm {
  numberOfWerewolves: string;
  traditonalRoles: string[];
  specialRoles: string[];
  showGameSummary: boolean;
  allowMultipleSelfHeals: boolean;
  selfModerated: boolean;
  nightStepSeconds: string;
}

export const EditRoomRoleSettings = ({
  roomRoleSettingsQuery,
}: {
  roomRoleSettingsQuery: ReturnType<typeof useRoomRoleSettings>;
}) => {
  const { t } = useTranslation();
  const { showToast } = useToaster();
  const roomId = useRoomId();
  const drawer = useDrawer({ closeOnInteractOutside: false });
  const { mutate, isPending: isUpdatingSettings } = useUpdateRoomRoleSettings({
    onSuccess: async () => {
      showToast({
        title: t("roleSettings.updated.title"),
        // description: t("roleSettings.updated.description"),
        type: "success",
        duration: 2500,
      });
    },
  });
  const { data, isRoleType } = useRoles();
  const { data: savedRoleSettings, isLoading: isRoomRoleSettingsLoading } =
    roomRoleSettingsQuery;
  const {
    control,
    handleSubmit,
    reset,
    formState: { isDirty: isFormDirty },
  } = useForm<EditRoomRoleSettingsForm>();
  const onSubmit: SubmitHandler<EditRoomRoleSettingsForm> = useCallback(
    (data) => {
      if (!savedRoleSettings) {
        showToast({
          title: t("roleSettings.error.title"),
          description: t("roleSettings.error.description"),
          type: "error",
        });
        return;
      }
      console.log(data);
      const request: RoomRoleSettingsDto = {
        id: savedRoleSettings.id,
        roomId: roomId,
        numberOfWerewolves: parseInt(data.numberOfWerewolves),
        selectedRoles: [
          ...data.traditonalRoles.map((val) => parseInt(val)),
          ...data.specialRoles.map((val) => parseInt(val)),
        ],
        showGameSummary: data.showGameSummary,
        allowMultipleSelfHeals: data.allowMultipleSelfHeals,
        selfModerated: data.selfModerated,
        nightStepSeconds: parseInt(data.nightStepSeconds),
      };
      void mutate(request, {
        onSuccess: () => {
          drawer.setOpen(false);
          reset(data);
        },
      });
    },
    [drawer, mutate, reset, roomId, savedRoleSettings, showToast, t]
  );

  const traditonalRoles = data.filter(
    (role) => role.roleType === RoleType.Traditional
  );
  const specialRoles = data.filter(
    (role) => role.roleType === RoleType.Special
  );

  return (
    <DrawerRootProvider
      value={drawer}
      onExitComplete={() => reset()}
      size={{ base: "full", md: "md" }}
      placement={DrawerPlacementForMobileDesktop}
    >
      <DrawerBackdrop />
      <DrawerTrigger asChild>
        <Button size="sm" w="full" variant="subtle" colorPalette="blue">
          <IconSettings /> {t("button.editSettings")}
        </Button>
      </DrawerTrigger>
      <form>
        <DrawerContent>
          <DrawerCloseTrigger />

          <DrawerHeader>
            <HStack gap={1}>
              <IconCards size={18} />
              <Text fontWeight={500} fontSize="lg">
                {t("common.roleSettings")}
              </Text>
            </HStack>
          </DrawerHeader>
          <DrawerBody>
            <Tabs.Root fitted defaultValue="roles">
              <Tabs.List>
                <Tabs.Trigger value="roles">
                  {t("roleSettings.roles")}
                </Tabs.Trigger>
                <Tabs.Trigger value="settings">
                  {t("roleSettings.settings")}
                </Tabs.Trigger>
              </Tabs.List>
              <Tabs.Content
                _open={{
                  animationName: "fade-in-from-bottom",
                  animationDuration: "500ms",
                }}
                value="roles"
              >
                <Stack w="full" gap={3}>
                  <Group>
                    <Field
                      label={
                        <Text textStyle="accent" fontWeight={600} fontSize="lg">
                          {t("roleSettings.numberOfWerewolves")}
                        </Text>
                      }
                    >
                      <Skeleton height={10} loading={isRoomRoleSettingsLoading}>
                        <Controller
                          name="numberOfWerewolves"
                          control={control}
                          defaultValue={savedRoleSettings?.numberOfWerewolves.toString()}
                          render={({ field }) => (
                            <SegmentedControl
                              name={field.name}
                              value={field.value}
                              onChange={field.onChange}
                              size="lg"
                              items={["1", "2", "3", "4"]}
                            />
                          )}
                        />
                      </Skeleton>
                    </Field>
                    <Image src={werewolfImg} alt="werewolf" w="64px" />
                  </Group>

                  <Separator flex="1" />
                  <Skeleton height="6rem" loading={isRoomRoleSettingsLoading}>
                    <Controller
                      name="traditonalRoles"
                      control={control}
                      defaultValue={savedRoleSettings?.selectedRoles
                        .filter((role) =>
                          isRoleType(role, RoleType.Traditional)
                        )
                        .map((role) => role.toString())}
                      render={({ field }) => (
                        <CheckboxGroup
                          name={field.name}
                          value={field.value}
                          onValueChange={field.onChange}
                        >
                          <Group>
                            <Text
                              textStyle="accent"
                              fontWeight={600}
                              fontSize="lg"
                            >
                              {t("roleSettings.traditionalRoles")}
                            </Text>
                            <RoleInformationDialog roles={traditonalRoles} />
                          </Group>

                          <SimpleGrid columns={{ base: 3, md: 4 }} gap="2">
                            {traditonalRoles.map((role) => (
                              <CheckboxCard
                                variant="surface"
                                align="center"
                                key={role.label}
                                icon={
                                  <img
                                    src={role.imgSrc}
                                    alt={role.label}
                                    width="36"
                                    height="36"
                                  />
                                }
                                label={
                                  <Text
                                    fontSize="lg"
                                    fontWeight="bold"
                                    textStyle="accent"
                                  >
                                    {role.label}
                                  </Text>
                                }
                                indicator={
                                  <Float placement="top-end" offset="1em">
                                    <CheckboxCardIndicator w="1rem" h="1rem" />
                                  </Float>
                                }
                                value={role.roleName.toString()}
                              />
                            ))}
                          </SimpleGrid>
                        </CheckboxGroup>
                      )}
                    />
                  </Skeleton>

                  <Separator flex="1" />
                  <Skeleton height="6rem" loading={isRoomRoleSettingsLoading}>
                    <Controller
                      name="specialRoles"
                      control={control}
                      defaultValue={savedRoleSettings?.selectedRoles
                        .filter((role) => isRoleType(role, RoleType.Special))
                        .map((role) => role.toString())}
                      render={({ field }) => (
                        <CheckboxGroup
                          name={field.name}
                          value={field.value}
                          onValueChange={field.onChange}
                        >
                          <Group>
                            <Text
                              textStyle="accent"
                              fontWeight={600}
                              fontSize="lg"
                            >
                              {t("roleSettings.specialRoles")}
                            </Text>
                            <RoleInformationDialog roles={specialRoles} />
                          </Group>
                          <Skeleton
                            w="full"
                            loading={isRoomRoleSettingsLoading}
                            minH={
                              isRoomRoleSettingsLoading ? "100px" : undefined
                            }
                          >
                            <SimpleGrid columns={{ base: 3, md: 4 }} gap="2">
                              {specialRoles.map((role) => (
                                <CheckboxCard
                                  variant="surface"
                                  align="center"
                                  key={role.label}
                                  icon={
                                    <img
                                      src={role.imgSrc}
                                      alt={role.label}
                                      width="36"
                                      height="36"
                                    />
                                  }
                                  indicator={
                                    <Float placement="top-end" offset="1em">
                                      <CheckboxCardIndicator
                                        w="1rem"
                                        h="1rem"
                                      />
                                    </Float>
                                  }
                                  label={
                                    <Text
                                      fontSize="lg"
                                      fontWeight="bold"
                                      textStyle="accent"
                                    >
                                      {role.label}
                                    </Text>
                                  }
                                  value={role.roleName.toString()}
                                />
                              ))}
                            </SimpleGrid>
                          </Skeleton>
                        </CheckboxGroup>
                      )}
                    />
                  </Skeleton>
                </Stack>
              </Tabs.Content>
              <Tabs.Content
                _open={{
                  animationName: "fade-in-from-bottom",
                  animationDuration: "500ms",
                }}
                value="settings"
              >
                <Stack gap={4}>
                  <ChakraField.Root>
                    <ChakraField.Label>
                      <Text>{t("roleSettings.selfModeratedSwitch.label")}</Text>
                    </ChakraField.Label>
                    <Skeleton
                      height={6}
                      width={12}
                      loading={isRoomRoleSettingsLoading}
                    >
                      <Controller
                        name="selfModerated"
                        control={control}
                        defaultValue={savedRoleSettings?.selfModerated}
                        render={({ field }) => (
                          <Switch
                            size="lg"
                            name={field.name}
                            checked={field.value}
                            onCheckedChange={({ checked }) =>
                              field.onChange(checked)
                            }
                            inputProps={{ onBlur: field.onBlur }}
                          />
                        )}
                      />
                    </Skeleton>
                    <ChakraField.HelperText>
                      {t("roleSettings.selfModeratedSwitch.description")}
                    </ChakraField.HelperText>
                  </ChakraField.Root>
                  <ChakraField.Root>
                    <ChakraField.Label>
                      <Text>{t("roleSettings.nightStepSeconds.label")}</Text>
                    </ChakraField.Label>
                    <Skeleton height={10} loading={isRoomRoleSettingsLoading}>
                      <Controller
                        name="nightStepSeconds"
                        control={control}
                        defaultValue={savedRoleSettings?.nightStepSeconds.toString()}
                        render={({ field }) => (
                          <SegmentedControl
                            name={field.name}
                            value={field.value}
                            onChange={field.onChange}
                            size="lg"
                            items={["10", "15", "20", "30", "45"]}
                          />
                        )}
                      />
                    </Skeleton>
                    <ChakraField.HelperText>
                      {t("roleSettings.nightStepSeconds.description")}
                    </ChakraField.HelperText>
                  </ChakraField.Root>
                  <ChakraField.Root>
                    <ChakraField.Label>
                      <Text>{t("roleSettings.gameSummarySwitch.label")}</Text>
                    </ChakraField.Label>
                    <Skeleton
                      height={6}
                      width={12}
                      loading={isRoomRoleSettingsLoading}
                    >
                      <Controller
                        name="showGameSummary"
                        control={control}
                        defaultValue={savedRoleSettings?.showGameSummary}
                        render={({ field }) => (
                          <Switch
                            size="lg"
                            name={field.name}
                            checked={field.value}
                            onCheckedChange={({ checked }) =>
                              field.onChange(checked)
                            }
                            inputProps={{ onBlur: field.onBlur }}
                          />
                        )}
                      />
                    </Skeleton>
                    <ChakraField.HelperText>
                      {t("roleSettings.gameSummarySwitch.description")}
                    </ChakraField.HelperText>
                  </ChakraField.Root>
                  <ChakraField.Root>
                    <ChakraField.Label>
                      <Text>
                        {t("roleSettings.allowSelfHealsSwitch.label")}{" "}
                      </Text>
                      {/* <Badge size="xs" colorPalette="yellow" variant="subtle">
                        {t("Coming Soon")}
                      </Badge> */}
                    </ChakraField.Label>
                    <Skeleton
                      height={6}
                      width={12}
                      loading={isRoomRoleSettingsLoading}
                    >
                      <Controller
                        defaultValue={savedRoleSettings?.allowMultipleSelfHeals}
                        name="allowMultipleSelfHeals"
                        control={control}
                        render={({ field }) => (
                          <Switch
                            size="lg"
                            name={field.name}
                            checked={field.value}
                            onCheckedChange={({ checked }) =>
                              field.onChange(checked)
                            }
                            inputProps={{ onBlur: field.onBlur }}
                          />
                        )}
                      />
                    </Skeleton>
                    <ChakraField.HelperText>
                      {t("roleSettings.allowSelfHealsSwitch.description")}{" "}
                    </ChakraField.HelperText>
                  </ChakraField.Root>
                </Stack>
              </Tabs.Content>
            </Tabs.Root>
          </DrawerBody>
          <DrawerFooter>
            <Button
              disabled={!isFormDirty}
              w="full"
              onClick={handleSubmit(onSubmit)}
              loading={isUpdatingSettings}
              type="submit"
            >
              {t("roleSettings.button.updateSettings")}
            </Button>
          </DrawerFooter>
        </DrawerContent>
      </form>
    </DrawerRootProvider>
  );
};

export default EditRoomRoleSettings;
