using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Repository;
using WerewolfParty_Server.Role;

namespace WerewolfParty_Server.Service;

public class GameService(
    RoomGameActionRepository roomGameActionRepository,
    PlayerRoomRepository playerRoomRepository,
    RoomRepository roomRepository,
    RoleSettingsRepository roleSettingsRepository,
    RoleFactory roleFactory,
    IMapper mapper)
{
    public void ProcessQueuedActions(string roomId)
    {
        var queuedActions = roomGameActionRepository.GetQueuedActionsForRoom(roomId);
        var playersRevivedSet = new HashSet<Guid>();
        var playersKilledSet = new HashSet<Guid>();
        foreach (var action in queuedActions)
        {
            switch (action.Action)
            {
                case ActionType.VotedOut:
                    playersRevivedSet.Add(action.PlayerId);
                    break;
                case ActionType.Investigate:
                    continue;
                    break;
                case ActionType.Kill:
                {
                    if (playersRevivedSet.Contains(action.AffectedPlayerId))
                    {
                        playersRevivedSet.Remove(action.AffectedPlayerId);
                    }
                    else playersKilledSet.Add(action.AffectedPlayerId);

                    break;
                }
                case ActionType.Revive:
                {
                    if (playersKilledSet.Contains(action.AffectedPlayerId))
                    {
                        playersKilledSet.Remove(action.AffectedPlayerId);
                    }
                    else playersRevivedSet.Add(action.AffectedPlayerId);

                    break;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        //Set killed players as dead
        playerRoomRepository.UpdatePlayerIsAliveStatus(playersRevivedSet.ToList(), false);
        roomGameActionRepository.ProcessActionsForRoom(roomId, queuedActions);
    }

    private void ResetRoomForNewGame(string roomId)
    {
        roomGameActionRepository.ClearAllActionsForRoom(roomId);
        var room = roomRepository.GetRoom(roomId);
        room.CurrentNight = 0;
        roomRepository.UpdateRoom(room);
        var players = playerRoomRepository.GetPlayersInRoom(roomId);
        foreach (var player in players)
        {
            player.isAlive = true;
            player.AssignedRole = null;
        }
        playerRoomRepository.UpdateGroupOfPlayersInRoom(players);
    }

    private bool IsEnoughPlayersForGame(string roomId)
    {
        var playersInLobby = playerRoomRepository.GetPlayersInRoom(roomId);
        var roleSettingsForRoom = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        var playerCountWithoutMod = playersInLobby.Count - 1;
        var playersNeededForGame = roleSettingsForRoom.SelectedRoles.Count + (int)roleSettingsForRoom.Werewolves;
        return playerCountWithoutMod >= playersNeededForGame;
    }

    public void StartGame(string roomId)
    {
        var canStartGame = IsEnoughPlayersForGame(roomId);
        if (!canStartGame)
        {
            throw new Exception("Not enough players for game");
        }
        ResetRoomForNewGame(roomId);
        ShuffleAndAssignRoles(roomId);
        var room = roomRepository.GetRoom(roomId);
        room.GameState = GameState.CardsDealt;
        roomRepository.UpdateRoom(room);
    }

    public RoleName? GetAssignedPlayerRole(string roomId,Guid playerId)
    {
        var playerInRoom = playerRoomRepository.GetPlayerInRoom(roomId,playerId);
        return playerInRoom.AssignedRole;
    }
    // public List<PlayerRoleDTO> GetAllAssignedPlayerRolesAndActions(string roomId)
    // {
    //     var currentModerator = roomRepository.GetRoom(roomId).CurrentModerator;
    //     var playersInRoom = playerRoomRepository.GetPlayersInRoom(roomId);
    //     var playersInRoomWithoutMod = playersInRoom.Where(p => p.PlayerGuid != currentModerator).ToList();
    //     return mapper.Map<List<PlayerRoleDTO>>(playersInRoomWithoutMod);
    // }

    public List<RoleActionDto> GetActionsForPlayerRole(string roomId, Guid playerId)
    {
        var playerDetails = playerRoomRepository.GetPlayerInRoom(roomId, playerId);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var playerRole = playerDetails.AssignedRole;
        if (!playerRole.HasValue)
        {
            throw new Exception("No role assigned");
        }
        var role = roleFactory.GetRole(playerRole.Value);
        return role.GetActions(priorActions, playerId);
    }

    public List<PlayerRoleActionDto> GetAllAssignedPlayerRolesAndActions(string roomId)
    {
        var currentMod = roomRepository.GetModeratorForRoom(roomId);
        var playersinRoom = playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, currentMod);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var roleActionList = new List<PlayerRoleActionDto>();
        foreach (var player in playersinRoom)
        {
            if (!player.AssignedRole.HasValue)
            {
                throw new Exception("No role assigned");
            }
            var role = roleFactory.GetRole(player.AssignedRole.Value);
            roleActionList.Add(
            new PlayerRoleActionDto(){
                Id = player.PlayerGuid,
                Nickname = player.NickName,
                AvatarIndex = player.AvatarIndex,
                Role = player.AssignedRole.Value,
                Actions = role.GetActions(priorActions, player.PlayerGuid)
            });
        }
return roleActionList;      
    }

    public GameState GetGameState(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        return room.GameState;
    }

    private List<PlayerRoleDTO> ShuffleAndAssignRoles(string roomId)
    {
        var roomModerator = roomRepository.GetModeratorForRoom(roomId);
        var playersInRoomWithoutMod = playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, roomModerator);
        playersInRoomWithoutMod = playersInRoomWithoutMod.Shuffle();
        var roomSettings = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        var roleCards = roomSettings.SelectedRoles;
        for (int i = 0; i < (int)roomSettings.Werewolves; i++)
        {
            roleCards.Add(RoleName.WereWolf);
        }

        playersInRoomWithoutMod = playersInRoomWithoutMod.Shuffle();
        for (int i = 0; i < playersInRoomWithoutMod.Count; i++)
        {
            var player = playersInRoomWithoutMod[i];
            if (i > roleCards.Count - 1)
            {
                player.AssignedRole = RoleName.Villager;
            }
            else
            {
                player.AssignedRole = roleCards[i];
            }
        }

        var assignedRoles = playerRoomRepository.UpdateGroupOfPlayersInRoom(playersInRoomWithoutMod);

        return mapper.Map<List<PlayerRoleDTO>>(assignedRoles);
    }
}