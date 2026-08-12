import { Card, Stack, Text } from "@chakra-ui/react";
import { Button } from "@/components/ui/button";
import { IconBell, IconDeviceMobileShare } from "@tabler/icons-react";
import { useTranslation } from "react-i18next";
import { usePushNotifications } from "@/hooks/usePushNotifications";

/**
 * Offers to turn on turn notifications, and explains the iOS installation step when that is
 * what stands in the way.
 *
 * Shows nothing at all once a device is subscribed, when the deployment has no push keys, or
 * on a browser that cannot do push — in every one of those cases the in-app prompt is already
 * doing the job and a permanent banner would just be noise.
 */
export const EnableNotificationsCard = () => {
  const { t } = useTranslation();
  const { status, subscribe, isSubscribing } = usePushNotifications();

  if (status === "subscribed" || status === "unconfigured" || status === "unsupported") {
    return null;
  }

  const isInstallStep = status === "needs-install";
  const isDenied = status === "denied";

  return (
    <Card.Root variant="subtle">
      <Card.Body>
        <Stack direction="row" align="center" gap={3}>
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
          {status === "prompt" && (
            <Button size="sm" loading={isSubscribing} onClick={() => void subscribe()}>
              {t("game.notifications.enable.button")}
            </Button>
          )}
        </Stack>
      </Card.Body>
    </Card.Root>
  );
};
