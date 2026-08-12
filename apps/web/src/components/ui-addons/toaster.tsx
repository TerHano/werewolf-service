"use client";

import {
  Toaster as ChakraToaster,
  Circle,
  Icon,
  Portal,
  Spinner,
  Stack,
  Toast,
} from "@chakra-ui/react";
import {
  IconAlertTriangle,
  IconCircleCheck,
  IconInfoCircle,
} from "@tabler/icons-react";
import React, { useMemo } from "react";
import { toaster } from "./createToaster";

type WerewolfToastType =
  | "success"
  | "error"
  | "loading"
  | "info"
  | "warning"
  | (string & {});

export const Toaster = () => {
  return (
    <Portal>
      <ChakraToaster toaster={toaster} insetInline={{ mdDown: "4" }}>
        {(toast) => (
          <Toast.Root
            alignItems="center"
            css={{ bgColor: "bg", color: "fg" }}
            width={{ md: "sm" }}
          >
            {toast.type === "loading" ? (
              <Spinner size="sm" color="blue.solid" />
            ) : (
              <ToasterIcon icon={toast.meta?.icon} toastType={toast.type} />
            )}
            <Stack gap="1" flex="1" maxWidth="100%">
              {toast.title && <Toast.Title>{toast.title}</Toast.Title>}
              {toast.description && (
                <Toast.Description>{toast.description}</Toast.Description>
              )}
            </Stack>
            {toast.action && (
              <Toast.ActionTrigger>{toast.action.label}</Toast.ActionTrigger>
            )}
            {/* <Toast.CloseTrigger /> */}
          </Toast.Root>
        )}
      </ChakraToaster>
    </Portal>
  );
};

const ToasterIcon = ({
  toastType,
  icon,
}: {
  toastType?: WerewolfToastType;
  icon?: React.ReactNode;
}) => {
  const toastCss = useMemo(() => {
    let typeCss;
    switch (toastType) {
      case "success":
        typeCss = { bgColor: "green.subtle", color: "green.fg" };
        break;
      case "error":
        typeCss = {
          bgColor: "red.subtle",
          color: "red.fg",
        };
        break;
      case "warning":
        typeCss = {
          bgColor: "yellow.subtle",
          color: "yellow.fg",
        };
        break;
      case "info":
        typeCss = {
          bgColor: "blue.subtle",
          color: "blue.fg",
        };
        break;
      default:
        typeCss = undefined;
        break;
    }
    return {
      ...typeCss,

      p: "5px",
      boxSizing: "initial",
    };
  }, [toastType]);

  const toastIcon = useMemo<React.ReactNode>(() => {
    if (icon) {
      return icon;
    }
    switch (toastType) {
      case "warning":
        return <IconAlertTriangle />;
      case "error":
        return <IconAlertTriangle />;
      case "success":
        return <IconCircleCheck />;
      default:
        return <IconInfoCircle />;
    }
  }, [icon, toastType]);

  return (
    <Circle css={toastCss}>
      <Icon size="md" borderRadius="none">
        {toastIcon}
      </Icon>
    </Circle>
  );
};
