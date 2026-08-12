import {
  Card,
  Group,
  HStack,
  Image,
  Input,
  Link,
  Separator,
  Stack,
  Text,
} from "@chakra-ui/react";
import { Field } from "../ui/field";
import { Button } from "../ui/button";
import { IconArrowRight, IconUsersGroup } from "@tabler/icons-react";
import campIcon from "../../assets/icons/lobby-icon.png";
import { useCreateRoom } from "@/hooks/useCreateRoom";
import { useCheckRoom } from "@/hooks/useCheckRoom";
import { SubmitHandler, useForm } from "react-hook-form";
import { useNavigate } from "@tanstack/react-router";
import { useTranslation } from "react-i18next";
import { useDocumentTitle } from "@uidotdev/usehooks";
import { useToaster } from "@/hooks/ui/useToaster";

type CheckRoomForm = {
  roomId: string;
};

export const MainMenu = () => {
  const { t } = useTranslation();
  const { showToast } = useToaster();
  const navigate = useNavigate();
  const {
    mutate: createNewRoom,
    isPending: isCreatingRoomPending,
    isSuccess: isCreatingRoomSuccess,
  } = useCreateRoom({
    onSuccess: async (newRoomId) => {
      navigate({ to: `/room/$roomId`, params: { roomId: newRoomId } });
    },
  });
  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<CheckRoomForm>();
  const onSubmit: SubmitHandler<CheckRoomForm> = (data) =>
    checkRoom({
      roomId: data.roomId.toUpperCase(),
    });
  const { mutate: checkRoom, isPending: isCheckingRoomPending } = useCheckRoom({
    onSuccess: async (doesExist, { roomId }) => {
      if (!doesExist) {
        showToast({
          type: "error",
          title: t("mainMenu.joinRoomToast.roomNotFound.title"),
          description: (
            <Text as="span">
              {t("mainMenu.joinRoomToast.roomNotFound.description", {
                roomId: roomId.toUpperCase(),
              })}
            </Text>
          ),
          duration: 3000,
          withDismissButton: true,
        });
      } else {
        navigate({
          to: `/room/$roomId`,
          params: { roomId: roomId.toUpperCase() },
        });
      }
    },
  });

  useDocumentTitle(`Werewolf Party`);

  return (
    <Stack align="center" w="100%" className="animate-fade-in-from-bottom">
      <Card.Root w="full" padding={3} className="animate-fade-in-from-bottom">
        <Stack alignItems="center" gap={3}>
          <Stack alignItems="center" gap={0}>
            <Image width={150} src={campIcon} />
            <Text
              textStyle="accent"
              fontSize="2.5rem"
              textAlign="center"
              lineHeight={1}
              fontWeight="bold"
            >
              {t("common.name")}
            </Text>
            <Text
              textAlign="center"
              textStyle="accent"
              fontSize="medium"
              color="gray.300"
            >
              {t("mainMenu.subTitle")}
            </Text>
          </Stack>
          <form onSubmit={handleSubmit(onSubmit)}>
            <Field
              invalid={!!errors.roomId}
              maxWidth={250}
              label={t("mainMenu.joinRoomInput.label")}
              helperText={
                <Text fontSize="x-small">
                  {t("mainMenu.joinRoomInput.helper")}
                </Text>
              }
              errorText={
                <Text fontSize="x-small">{errors.roomId?.message}</Text>
              }
            >
              <Group attached>
                <Input
                  size="lg"
                  {...register("roomId", {
                    required: {
                      value: true,
                      message: t("mainMenu.joinRoomInput.required"),
                    },
                    minLength: {
                      value: 5,
                      message: t("mainMenu.joinRoomInput.minLength"),
                    },
                    maxLength: 5,
                  })}
                  style={{ textTransform: "uppercase" }}
                  maxLength={5}
                  placeholder="(Ex: 38VF5)"
                />
                <Button
                  loading={isCheckingRoomPending}
                  type="submit"
                  padding={0}
                >
                  <IconArrowRight />
                </Button>
              </Group>
            </Field>
          </form>
          <HStack alignItems="center" w="full">
            <Separator flex="1" variant="dashed" />
            <Text textStyle="accent" flexShrink="0">
              {t("mainMenu.or")}
            </Text>
            <Separator flex="1" variant="dashed" />
          </HStack>
          <Button
            loading={isCreatingRoomPending || isCreatingRoomSuccess}
            onClick={() => {
              createNewRoom();
            }}
            size="md"
          >
            {t("mainMenu.createNewRoom")} <IconUsersGroup />
          </Button>
          <Stack align="center" gap={0} direction="column">
            <Text color="gray.500" textStyle="accent">
              {t("mainMenu.developedBy")}
            </Text>
            <Text color="gray.500" textStyle="accent">
              {t("mainMenu.iconsBy")}{" "}
              <Link
                target="_blank"
                href="https://icons8.com/icon/WlXKRWqXdjfz/werewolf"
                // eslint-disable-next-line i18next/no-literal-string
              >
                Icons8
              </Link>{" "}
              {t("mainMenu.and")}{" "}
              <Link
                target="_blank"
                href="https://www.flaticon.com/authors/graphiqa"
                // eslint-disable-next-line i18next/no-literal-string
              >
                Graphiqa
              </Link>
            </Text>
          </Stack>
        </Stack>
      </Card.Root>
    </Stack>
  );
};
