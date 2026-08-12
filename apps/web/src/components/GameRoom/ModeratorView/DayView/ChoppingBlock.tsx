import { Button } from "@/components/ui/button";
import { useRoomId } from "@/hooks/useRoomId";
import { useAllPlayerRoles } from "@/hooks/useAllPlayerRoles";
import { useVotePlayerOut } from "@/hooks/useVotePlayerOut";
import { useTranslation } from "react-i18next";
import {
  Card,
  Group,
  Separator,
  SimpleGrid,
  Stack,
  Text,
} from "@chakra-ui/react";
import { lazy, useState } from "react";
import { PlayerList } from "@/components/GameRoom/ModeratorView/NightView/ActionModals/PlayerList";
import { InfoTip } from "@/components/ui/toggle-tip";

const KilledPlayersBanner = lazy(() => import("./KilledPlayersBanner"));

export const ChoppingBlock = () => {
  const roomId = useRoomId();

  const { t } = useTranslation();

  const { data: allPlayerRoles } = useAllPlayerRoles(roomId);
  const alivePlayers = allPlayerRoles?.filter((player) => player.isAlive);
  const [selectedPlayer, setSelectedPlayer] = useState<number | undefined>();
  const { mutate: votePlayerOutMutate, isPending: isVotePending } =
    useVotePlayerOut();

  return (
    <Stack direction="column" gap={6}>
      <KilledPlayersBanner />
      <Separator flex="1" />
      <Card.Root
        animationDelay="moderate"
        className="animate-fade-in-from-bottom"
      >
        <Card.Body>
          <Card.Title>
            <Group>
              <Text textStyle="accent" fontSize="xl">
                {t("game.choppingBlock.vote.title")}
              </Text>
              <InfoTip
                size="lg"
                modal
                content={
                  <Text fontSize="1em" lineHeight={1.4} width={300}>
                    {t("game.choppingBlock.vote.helper")}
                  </Text>
                }
              />
            </Group>
          </Card.Title>
          <Stack mt={4} gap={3}>
            <PlayerList
              selectedPlayer={selectedPlayer}
              players={alivePlayers ?? []}
              onPlayerSelect={setSelectedPlayer}
            />

            <SimpleGrid columns={2} gap={4}>
              <Button
                w="100%"
                variant="subtle"
                onClick={() => {
                  votePlayerOutMutate({
                    roomId,
                    playerRoleId: undefined,
                  });
                }}
              >
                {t("game.choppingBlock.vote.abstain")}
              </Button>
              <Button
                w="100%"
                disabled={!selectedPlayer}
                loading={isVotePending}
                onClick={() => {
                  votePlayerOutMutate({
                    roomId,
                    playerRoleId: selectedPlayer,
                  });
                }}
              >
                {t("game.choppingBlock.vote.lynch")}
              </Button>
            </SimpleGrid>
          </Stack>
        </Card.Body>
      </Card.Root>
    </Stack>
  );
};
export default ChoppingBlock;
