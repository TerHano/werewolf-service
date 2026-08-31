import { Card, Stack, Text } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { IconBell, IconDeviceMobileShare } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";
import { usePushNotifications } from "@/hooks/usePushNotifications";
import { InstallOnIosDialog } from "./InstallOnIosDialog";
import { useState } from "react";

/**
 * Offers to turn on turn notifications, and explains the iOS installation step when that is
 * what stands in the way.
 *
 * Shows nothing at all once a device is subscribed, when the deployment has no push keys, or
 * on a browser that cannot do push — in every one of those cases the in-app prompt is already
 * doing the job and a permanent banner would just be noise.
 *
 * Deliberately a card and not a dialog. It leads with what the player gets rather than with
 * the work, it waits to be tapped, and because it is driven by push status rather than by a
 * one-time dismissal it is still there later, when the game starts and a buzz is suddenly
 * worth having.
 */
export const EnableNotificationsCard = () => {
  const { t } = useTranslation();
  const { status, subscribe, isSubscribing } = usePushNotifications();
  const [isInstallDialogOpen, setInstallDialogOpen] = useState(false);

  if (
    status === "subscribed" ||
    status === "unconfigured" ||
    status === "unsupported"
  ) {
    return null;
  }

  const isInstallStep = status === "needs-install";
  const isDenied = status === "denied";

  return (
    <Card.Root variant="subtle">
      <Card.Body>
        {/* The button drops onto its own row on a phone. Kept inline it competes with the
            description for a 375px line and squeezes it into a column six words tall. */}
        <Stack
          direction={{ base: "column", sm: "row" }}
          align={{ base: "stretch", sm: "center" }}
          gap={3}
        >
          <Stack direction="row" align="center" gap={3} flex="1">
            {isInstallStep ? <IconDeviceMobileShare /> : <IconBell />}
            <Stack gap={0} flex="1">
              <Text textStyle="accent">
                {isInstallStep
                  ? t("game.notifications.install.title")
                  : isDenied
                    ? t("game.notifications.denied.title")
                    : t("game.notifications.enable.title")}
              </Text>
              <Text color="dimmed" fontSize="sm">
                {isInstallStep
                  ? t("game.notifications.install.description")
                  : isDenied
                    ? t("game.notifications.denied.description")
                    : t("game.notifications.enable.description")}
              </Text>
            </Stack>
          </Stack>
          {status === "prompt" && (
            <Button
              size="sm"
              w={{ base: "full", sm: "auto" }}
              loading={isSubscribing}
              onClick={() => void subscribe()}
            >
              {t("game.notifications.enable.button")}
            </Button>
          )}
          {isInstallStep && (
            <Button
              size="sm"
              variant="subtle"
              flexShrink={0}
              w={{ base: "full", sm: "auto" }}
              onClick={() => setInstallDialogOpen(true)}
            >
              {t("game.notifications.install.button")}
            </Button>
          )}
        </Stack>
      </Card.Body>
      <InstallOnIosDialog
        open={isInstallDialogOpen}
        onClose={() => setInstallDialogOpen(false)}
      />
    </Card.Root>
  );
};
