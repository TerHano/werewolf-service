import { Box, Stack, Text } from "@chakra-ui/react";
import {
  IconBell,
  IconDeviceMobileShare,
  IconPlus,
  IconShare2,
  IconSquarePlus,
} from "@tabler/icons-react";
import { ReactNode } from "react";
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

const Step = ({ icon, children }: { icon: ReactNode; children: ReactNode }) => (
  <Stack direction="row" align="center" gap={3}>
    <Box color="fg.muted" flexShrink={0}>
      {icon}
    </Box>
    <Text fontSize="sm">{children}</Text>
  </Stack>
);

/**
 * The Safari steps for adding the game to the home screen.
 *
 * iOS is a special case rather than a preference: Safari delivers Web Push only to a site
 * launched from the home screen, so an iPhone player who just follows the link cannot be told
 * it is their turn at all.
 *
 * Opened on request from EnableNotificationsCard, never on its own. Someone who has just
 * arrived in a room has no reason yet to want this, and a modal that opens itself asks them to
 * do a chore before it has said what the chore buys. The card makes the offer; this explains
 * how to take it.
 */
export const InstallOnIosDialog = ({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) => {
  const { t } = useTranslation();

  return (
    <DialogRoot
      open={open}
      onOpenChange={(event) => {
        if (!event.open) onClose();
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
          <Button variant="subtle" onClick={onClose}>
            {t("install.done")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </DialogRoot>
  );
};
