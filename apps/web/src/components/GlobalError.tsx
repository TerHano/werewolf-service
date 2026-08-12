import { Card, Image, Stack, Text } from "@chakra-ui/react";
import { ErrorComponentProps } from "@tanstack/react-router";
import { Button } from "./ui/button";
import errorImg from "@/assets/icons/error.png";
import { useTranslation } from "react-i18next";

// eslint-disable-next-line @typescript-eslint/no-unused-vars
export const GlobalError = (_props: ErrorComponentProps) => {
  const { t } = useTranslation();
  return (
    <Card.Root className="animate-fade-in-from-bottom" m="1rem" p="1rem">
      <Stack gap={4} align="center">
        <Stack align="center" gap={0}>
          <Image width="10rem" height="10rem" src={errorImg} />
          <Text textStyle="accent" fontSize="2xl">
            {t("global_error.title")}
          </Text>
          <Text textStyle="accent" fontSize="lg">
            {t("global_error.description")}
          </Text>
        </Stack>
        <Button onClick={() => window.location.reload()} colorScheme="blue">
          {t("button.reload")}
        </Button>
      </Stack>
    </Card.Root>
  );
};
