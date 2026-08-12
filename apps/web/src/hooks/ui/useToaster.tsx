import { toaster } from "@/components/ui-addons/createToaster";
import React, { useCallback } from "react";
import { useTranslation } from "react-i18next";

type WerewolfToastType = "success" | "error" | "loading" | "info" | "warning";

interface ToastOptions {
  id?: string;
  title?: React.ReactNode;
  description?: React.ReactNode;
  type?: WerewolfToastType;
  duration?: number;
  icon?: React.ReactNode;
  withDismissButton?: boolean;
}

export const useToaster = () => {
  type toasterCreate = Parameters<typeof toaster.create>[0];
  const { t } = useTranslation();
  const showToast = useCallback(
    (options: ToastOptions) => {
      const { withDismissButton, icon, duration, ...rest } = options;
      const _options: toasterCreate = {
        ...rest,
        duration: duration ?? 3000,
        meta: {
          icon: icon,
        },
        action: withDismissButton
          ? {
              label: t("toast.ok"),
              onClick: () => {},
            }
          : undefined,
      };

      toaster.create({ ..._options });
    },
    [t]
  );

  return { showToast };
};
