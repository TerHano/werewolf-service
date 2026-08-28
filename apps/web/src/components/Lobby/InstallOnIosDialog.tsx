import { Box, Stack, Text } from "@chakra-ui/react";
import {
  IconBell,
  IconDeviceMobileShare,
  IconPlus,
  IconShare2,
  IconSquarePlus,
} from "@tabler/icons-react";
import { ReactNode, useEffect, useState } from "react";
import { useTranslation } from "react-i18next";
import { Button } from "@/components/ui/button";
import {
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogRoot,
  DialogTitle,
} from "@/components/ui/dialog";
import { useIosInstall } from "@/hooks/useIosInstall";

const Step = ({ icon, children }: { icon: ReactNode; children: ReactNode }) => (
  <Stack direction="row" align="center" gap={3}>
    <Box color="fg.muted" flexShrink={0}>
      {icon}
    </Box>
    <Text fontSize="sm">{children}</Text>
  </Stack>
);

/**
 * Explains adding the game to the home screen, on the one platform where it is worth
 * interrupting someone to say so.
 *
 * iOS is a special case rather than a preference: Safari delivers Web Push only to a site
 * launched from the home screen, so an iPhone player who just follows the link cannot be told
 * it is their turn at all. That is the reason this exists, and the reason the dialog leads
 * with notifications rather than with tidiness.
 *
 * Shown once, in the lobby, where people are already waiting. Not during a game — a modal over
 * somebody's night turn would be worse than no notifications at all — and never again once
 * dismissed.
 */
export const InstallOnIosDialog = () => {
  const { t } = useTranslation();
  const { canInstall, dismiss } = useIosInstall();
  const [isOpen, setOpen] = useState(false);

  useEffect(() => {
    if (canInstall) setOpen(true);
  }, [canInstall]);

  if (!canInstall) return null;

  const close = () => {
    setOpen(false);
    dismiss();
  };

  return (
    <DialogRoot
      open={isOpen}
      onOpenChange={(event) => {
        if (!event.open) close();
      }}
    >
      <DialogContent>
        <DialogHeader>
          <DialogTitle>
            <Stack direction="row" align="center" gap={2}>
              <IconDeviceMobileShare size={20} />
              <Text textStyle="accent" fontSize="xl">
                {t("install.title")}
              </Text>
            </Stack>
          </DialogTitle>
        </DialogHeader>

        <DialogBody>
          <Stack gap={4}>
            <Text fontSize="sm" color="dimmed">
              {t("install.why")}
            </Text>

            <Stack gap={3}>
              <Step icon={<IconShare2 size={18} />}>{t("install.step1")}</Step>
              <Step icon={<IconSquarePlus size={18} />}>
                {t("install.step2")}
              </Step>
              <Step icon={<IconPlus size={18} />}>{t("install.step3")}</Step>
            </Stack>

            <Stack direction="row" align="center" gap={3} color="fg.muted">
              <IconBell size={18} />
              <Text fontSize="xs">{t("install.afterwards")}</Text>
            </Stack>
          </Stack>
        </DialogBody>

        <DialogFooter>
          <Button variant="subtle" onClick={close}>
            {t("install.dismiss")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </DialogRoot>
  );
};
