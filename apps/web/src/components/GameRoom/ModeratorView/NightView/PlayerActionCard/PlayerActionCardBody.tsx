import { VStack, Image, Stack } from "@chakra-ui/react";
import { IconSkull, IconUser } from "@tabler/icons-react";
import { PlayerRoleWithDetails } from "../NightCall";
import { SkeletonCircle } from "@/components/ui-addons/skeleton";
import { Tag } from "@/components/ui/tag";

export const PlayerActionCardBody = ({
  playerDetails,
}: {
  playerDetails: PlayerRoleWithDetails[];
}) => {
  const roleImg = playerDetails[0]?.roleInfo.imgSrc;

  return (
    <VStack
      animationDelay="moderate"
      className="animate-fade-in-from-bottom"
      gap={1}
    >
      <SkeletonCircle loading={!roleImg} size="8rem">
        <Image height="8rem" src={roleImg} />
      </SkeletonCircle>
      <Stack direction="row" gap={1}>
        {playerDetails.map((playerDetail) =>
          !playerDetail.isAlive ? (
            <Tag
              key={playerDetail.id}
              startElement={<IconSkull size={12} />}
              colorPalette="red"
            >
              {playerDetail.nickname}
            </Tag>
          ) : (
            <Tag key={playerDetail.id} startElement={<IconUser size={12} />}>
              {playerDetail.nickname}
            </Tag>
          )
        )}
      </Stack>
    </VStack>
  );
};
