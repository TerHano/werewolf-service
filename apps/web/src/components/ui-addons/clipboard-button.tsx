import {
  IconButton,
  HStack,
  Clipboard as ChakraClipboard,
  IconButtonProps,
  Text,
  ButtonProps,
  Button,
} from "@chakra-ui/react";
import { IconClipboard, IconClipboardCheck } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";

interface BaseClipboardButtonProps {
  value: string;
  onCopy?: () => void;
}

export interface ClipboardIconButtonProps extends BaseClipboardButtonProps {
  label: string;
  iconButton: IconButtonProps;
}

export interface ClipboardButtonProps extends BaseClipboardButtonProps {
  children: React.ReactNode;
  button?: ButtonProps;
}

export const ClipboardIconButton = ({
  iconButton,
  onCopy,
  value,
  label,
}: ClipboardIconButtonProps) => {
  const { t } = useTranslation();
  return (
    <ChakraClipboard.Root
      onStatusChange={({ copied }) => {
        if (copied) {
          if (onCopy) {
            onCopy();
          }
        }
      }}
      timeout={1000}
      value={value}
    >
      <ChakraClipboard.Trigger asChild>
        <ChakraClipboard.Indicator
          copied={
            <IconButton {...iconButton} colorPalette="green">
              <HStack p={3}>
                <Text>{t("button.copied")}</Text>
                <IconClipboardCheck />
              </HStack>
            </IconButton>
          }
        >
          <IconButton {...iconButton}>
            <HStack p={3}>
              <Text>{label}</Text>
              <IconClipboard />
            </HStack>
          </IconButton>
        </ChakraClipboard.Indicator>
      </ChakraClipboard.Trigger>
    </ChakraClipboard.Root>
  );
};

export const ClipboardButton = ({
  button,
  children,
  value,
  onCopy,
}: ClipboardButtonProps) => {
  const { t } = useTranslation();
  return (
    <ChakraClipboard.Root
      onStatusChange={({ copied }) => {
        if (copied) {
          if (onCopy) {
            onCopy();
          }
        }
      }}
      timeout={1000}
      value={value}
    >
      <ChakraClipboard.Trigger asChild>
        <ChakraClipboard.Indicator
          copied={
            <Button {...button} colorPalette="green">
              <HStack p={3}>
                <Text>{t("button.copied")}</Text>
                <IconClipboardCheck />
              </HStack>
            </Button>
          }
        >
          <Button {...button}>{children}</Button>
        </ChakraClipboard.Indicator>
      </ChakraClipboard.Trigger>
    </ChakraClipboard.Root>
  );
};
