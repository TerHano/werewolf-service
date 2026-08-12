import {
  Button,
  DialogRootProvider,
  IconButton,
  useDialog,
} from "@chakra-ui/react";
import {
  DialogBody,
  DialogContent,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "../ui/dialog";
import { RoleInfoList } from "./RoleInfoList";
import { RoleInfo } from "@/hooks/useRoles";
import { useTranslation } from "react-i18next";
import { IconQuestionMark } from "@tabler/icons-react";

interface RoleInformationDialogProps {
  roles: RoleInfo[];
}

export const RoleInformationDialog = ({
  roles,
}: RoleInformationDialogProps) => {
  const { t } = useTranslation();
  const dialog = useDialog();
  return (
    <DialogRootProvider value={dialog} size="md" scrollBehavior="inside">
      <DialogTrigger asChild>
        <IconButton borderRadius="full" variant="subtle" size="2xs">
          <IconQuestionMark />
        </IconButton>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{t("common.roleInformation")}</DialogTitle>
        </DialogHeader>
        <DialogBody>
          <RoleInfoList roles={roles} />
        </DialogBody>
        <DialogFooter>
          <Button w="full" onClick={() => dialog.setOpen(false)}>
            {t("Close")}
          </Button>
        </DialogFooter>
      </DialogContent>
    </DialogRootProvider>
  );
};
export default RoleInformationDialog;
