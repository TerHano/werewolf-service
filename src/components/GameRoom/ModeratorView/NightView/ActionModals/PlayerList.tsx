import { Badge, defineStyle, SimpleGrid, Stack } from "@chakra-ui/react";
import { usePlayerAvatar } from "@/hooks/usePlayerAvatar";
import { PlayerDto } from "@/dto/PlayerDto";
import { Avatar } from "@/components/ui/avatar";
import { PlayerRoleActionDto } from "@/dto/PlayerRoleActionDto";
import { GamePlayerDto } from "@/dto/GamePlayerDto";

export interface PlayerListProps {
  // GamePlayerDto is the self-moderated equivalent: same id/nickname/avatarIndex shape, keyed
  // by player role id, with no role attached.
  players: PlayerDto[] | PlayerRoleActionDto[] | GamePlayerDto[];
  selectedPlayer: number | undefined;
  onPlayerSelect: (selectedPlayerId: number | undefined) => void;
}

export const PlayerList = ({
  players,
  selectedPlayer,
  onPlayerSelect,
}: PlayerListProps) => {
  const { getAvatarImageSrcForIndex } = usePlayerAvatar();
  const ringCss = defineStyle({
    outlineWidth: "2px",
    outlineColor: "blue.500",
    outlineOffset: "2px",
    outlineStyle: "solid",
  });
  return (
    // <SimpleGrid columns={{ base: 2, xs: 3, sm: 5, md: 5 }} gap={5}>
    //   {players.map((player) => {
    //     return (
    //       <Stack align="center" gap={1}>
    //         <Avatar.Root
    //           variant="subtle"
    //           borderRadius="xl"
    //           size="2xl"
    //           onClick={() => onPlayerSelect(player.id)}
    //           key={player.id}
    //           style={{ cursor: "pointer" }}
    //           css={selectedPlayer === player.id ? ringCss : undefined}
    //         >
    //           <Avatar.Image
    //             marginTop={1}
    //             src={getAvatarImageSrcForIndex(player.avatarIndex)}
    //           />
    //         </Avatar.Root>
    //         <Badge
    //           colorPalette={selectedPlayer === player.id ? "blue" : undefined}
    //         >
    //           {player.nickname}
    //         </Badge>
    //       </Stack>
    //     );
    //   })}
    // </SimpleGrid>
    <SimpleGrid columns={{ base: 2, xs: 3, sm: 5, md: 5 }} gap={5}>
      {players?.map((player) => {
        return (
          <Stack key={player.id} gap={0} align="center">
            <Avatar
              size="2xl"
              shape="rounded"
              src={getAvatarImageSrcForIndex(player.avatarIndex)}
              colorPalette={selectedPlayer === player.id ? "blue" : undefined}
              style={{ cursor: "pointer" }}
              onClick={() => {
                if (selectedPlayer === player.id) {
                  onPlayerSelect(undefined);
                  return;
                }
                onPlayerSelect(player.id);
              }}
              css={selectedPlayer === player.id ? ringCss : undefined}
            />
            <Badge
              colorPalette={selectedPlayer === player.id ? "blue" : undefined}
            >
              {player.nickname}
            </Badge>
          </Stack>
        );
      })}
    </SimpleGrid>
  );
};
