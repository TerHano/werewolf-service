import { useAssignedRole } from "@/hooks/useAssignedRole";
import { useRoomId } from "@/hooks/useRoomId";
import { Card, VStack } from "@chakra-ui/react";
import { WaitingPlayerCard } from "@/components/GameRoom/PlayerView/WaitingPlayerCard";
import { PlayerRoleCard } from "@/components/GameRoom/PlayerView/PlayerRoleCard";
import { SkeletonCircle, SkeletonText } from "@/components/ui-addons/skeleton";
import { useRoles } from "@/hooks/useRoles";

export const PlayerView = () => {
  const roomId = useRoomId();
  const { data: assignedRole, isLoading: isAssignedRoleLoading } =
    useAssignedRole(roomId);

  const { getRole } = useRoles();
  const roleInfo = getRole(assignedRole);

  const isPlayerInWaitingRoom = !roleInfo;

  return (
    <Card.Root className="animate-fade-in-from-bottom">
      <Card.Body>
        <PlayerViewLoading loading={isAssignedRoleLoading}>
          {isPlayerInWaitingRoom ? (
            <WaitingPlayerCard />
          ) : (
            <PlayerRoleCard roleInfo={roleInfo} />
          )}
        </PlayerViewLoading>
      </Card.Body>
    </Card.Root>
  );
};

const PlayerViewLoading = ({
  loading,
  children,
}: {
  loading: boolean;
  children: React.ReactNode;
}) => {
  if (loading) {
    return (
      <VStack gap={4}>
        <SkeletonCircle loading size="10rem" />
        <SkeletonText loading noOfLines={4} />
      </VStack>
    );
  }
  return children;
};
