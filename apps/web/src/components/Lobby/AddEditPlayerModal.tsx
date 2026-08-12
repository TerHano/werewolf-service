import { lazy, Suspense, useEffect, useRef, useState } from "react";
import {
  DialogBackdrop,
  DialogBody,
  DialogCloseTrigger,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "../ui/dialog";
import { useTranslation } from "react-i18next";
import { Button } from "../ui/button";
import { Field } from "../ui/field";
import {
  Alert,
  DialogRootProvider,
  IconButton,
  Input,
  Skeleton,
  Text,
  useDialog,
  VStack,
} from "@chakra-ui/react";
import { AddEditPlayerDetailsDto } from "@/dto/AddEditPlayerDetailsDto";
import { IconPencil } from "@tabler/icons-react";
import { useForm, SubmitHandler } from "react-hook-form";
import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { useCurrentPlayer } from "@/hooks/useCurrentPlayer";
import { useRoomId } from "@/hooks/useRoomId";
import { useGameState } from "@/hooks/useGameState";
import { GameState } from "@/enum/GameState";
import { useToaster } from "@/hooks/ui/useToaster";
import { SkeletonCircle, SkeletonComposed } from "../ui-addons/skeleton";

const AvatarScrollPicker = lazy(
  () => import("@/components/Lobby/AvatarScrollPicker")
);

interface AddEditPlayerModalProps {
  isEdit?: boolean;
  submitCallback: (playerDetails: AddEditPlayerDetailsDto) => Promise<void>;
}

type AddEditPlayerModalForm = {
  nickname: string;
  avatarIndex: number;
};

const alphaNumericalPattern = /^[a-zA-Z0-9]*$/;

export const AddEditPlayerModal = ({
  isEdit = false,
  submitCallback,
}: AddEditPlayerModalProps) => {
  const roomId = useRoomId();
  const { showToast } = useToaster();
  const { t } = useTranslation();
  const { data: currentPlayer, isLoading: isCurrentPlayerLoading } =
    useCurrentPlayer(roomId, { enabled: isEdit });
  const { data: gameState } = useGameState(roomId);
  const { data: avatarNames } = usePlayerAvatar();

  const [isSubmitLoading, setSubmitLoading] = useState(false);
  const {
    getValues,
    register,
    handleSubmit,
    formState: { errors },
    setValue,
  } = useForm<AddEditPlayerModalForm>({
    defaultValues: {
      nickname: "",
      avatarIndex: getRandomInt(0, avatarNames.length),
    },
  });

  const dialog = useDialog({
    defaultOpen: !isEdit,
    closeOnEscape: isEdit,
    closeOnInteractOutside: false,
    initialFocusEl: () => focusRef.current,
  });

  const onSubmit: SubmitHandler<AddEditPlayerModalForm> = (data) => {
    setSubmitLoading(true);
    submitCallback({ ...data, roomId }).finally(() => {
      dialog.setOpen(false);
      setSubmitLoading(false);
      if (isEdit) {
        showToast({
          type: "success",
          title: t("addEditPlayerModal.playerDetailsUpdated"),
          withDismissButton: true,
        });
      }
    });
  };
  const focusRef = useRef<HTMLInputElement | null>(null);
  const { ref, ...nicknameField } = register("nickname", {
    required: {
      value: true,
      message: t("addEditPlayerModal.nicknameRequired"),
    },
    maxLength: {
      value: 10,
      message: t("addEditPlayerModal.nicknameLength"),
    },
    minLength: {
      value: 3,
      message: t("addEditPlayerModal.nicknameLength"),
    },
    validate: (val) => {
      if (!alphaNumericalPattern.test(val)) {
        return t("addEditPlayerModal.nicknamePattern");
      }
    },
  });

  useEffect(() => {
    if (currentPlayer) {
      setValue("nickname", currentPlayer.nickname);
      setValue("avatarIndex", currentPlayer.avatarIndex);
    }
  }, [currentPlayer, setValue]);

  return (
    <>
      <DialogRootProvider size="xs" value={dialog}>
        <DialogBackdrop />
        {isEdit && (
          <DialogTrigger>
            <IconButton size="xs" variant="outline">
              <IconPencil />
            </IconButton>
          </DialogTrigger>
        )}
        <DialogContent>
          {isEdit ? <DialogCloseTrigger /> : null}
          <DialogHeader>
            <DialogTitle>
              <Text textStyle="accent">
                {isEdit
                  ? t("addEditPlayerModal.updateDetails")
                  : t("addEditPlayerModal.addDetails")}
              </Text>
            </DialogTitle>
          </DialogHeader>
          <DialogBody>
            {
              //Warn player if they are joining a game in progress
            }
            {!isEdit && gameState === GameState.CardsDealt ? (
              <Alert.Root mb={4} size="sm" status="info">
                <Alert.Indicator />
                <Alert.Content>
                  <Alert.Title>{t("lobby.gameInProgress")}</Alert.Title>
                  <Alert.Description>
                    {t("lobby.waitingRoomMsg")}
                  </Alert.Description>
                </Alert.Content>
              </Alert.Root>
            ) : null}
            <form id="player-details-form" onSubmit={handleSubmit(onSubmit)}>
              <Suspense
                fallback={
                  <VStack gap={4}>
                    <SkeletonCircle loading size="3rem" />
                    <Skeleton height={4} w="full" />
                  </VStack>
                }
              >
                <SkeletonComposed
                  skeleton={
                    <VStack gap={4}>
                      <SkeletonCircle loading size="3rem" />
                      <Skeleton height={4} w="full" />
                    </VStack>
                  }
                  loading={isCurrentPlayerLoading}
                >
                  <VStack gap={4}>
                    <Field label={t("addEditPlayerModal.avatar")}>
                      <AvatarScrollPicker
                        setAvatarIndex={(index) => {
                          setValue("avatarIndex", index);
                        }}
                        initialAvatarIndex={
                          currentPlayer?.avatarIndex ??
                          getValues("avatarIndex") ??
                          0
                        }
                      />
                    </Field>

                    <Field
                      invalid={!!errors.nickname}
                      errorText={errors.nickname?.message}
                      helperText={
                        <Text fontSize="small">
                          {t("addEditPlayerModal.nicknameHelper")}
                        </Text>
                      }
                      defaultValue={currentPlayer?.nickname}
                      label={t("addEditPlayerModal.nickname")}
                    >
                      <Input
                        size="lg"
                        {...nicknameField}
                        ref={(e) => {
                          ref(e);
                          focusRef.current = e;
                        }}
                      />
                    </Field>
                  </VStack>
                </SkeletonComposed>
              </Suspense>
            </form>
          </DialogBody>
          <DialogFooter>
            <Button
              loading={isSubmitLoading}
              disabled={isSubmitLoading}
              form="player-details-form"
              type="submit"
            >
              {isEdit
                ? t("addEditPlayerModal.updateDetails")
                : t("button.joinRoom")}
            </Button>
          </DialogFooter>
        </DialogContent>
      </DialogRootProvider>
    </>
  );
};

function getRandomInt(min: number, max: number) {
  const minCeiled = Math.ceil(min);
  const maxFloored = Math.floor(max);
  return Math.floor(Math.random() * (maxFloored - minCeiled) + minCeiled);
}
