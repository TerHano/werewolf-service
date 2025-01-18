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
    private void ProcessQueuedActions(string roomId)
    {
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var playerRoles = playerRoomRepository.GetPlayersInRoomWithARole(roomId);
        var room = roomRepository.GetRoom(roomId);
        var playersRevivedSet = new HashSet<Guid>();
        var playersKilledSet = new HashSet<Guid>();
        var playersDeadSet = new HashSet<Guid>();
        var actionsQueuedForNextNight = new List<RoomGameActionEntity>();

        foreach (var action in queuedActions)
        {
            switch (action.Action)
            {
                case ActionType.Investigate:
                    break;
                case ActionType.Suicide:
                    playersDeadSet.Add(action.AffectedPlayerId);
                    break;
                case ActionType.WerewolfKill:
                case ActionType.Kill:
                {
                    //If player has been revived, they cannot be killed this night
                    if (playersRevivedSet.Contains(action.AffectedPlayerId)) continue;
                    playersKilledSet.Add(action.AffectedPlayerId);
                    break;
                }
                case ActionType.VigilanteKill:
                {
                    if (playersRevivedSet.Contains(action.AffectedPlayerId)) continue;
                    playersKilledSet.Add(action.AffectedPlayerId);
                    var killedPlayer = playerRoles.Find((player) => player.PlayerGuid.Equals(action.AffectedPlayerId));
                    if (killedPlayer == null) throw new Exception("Player not found");
                    if (killedPlayer.AssignedRole != RoleName.WereWolf)
                    {
                        if (!action.PlayerId.HasValue)
                        {
                            throw new Exception("Vigilante action must have a player id");
                        }
                        //Vigilante will be set to be killed next night;
                        var vigilanteSuicideAction = new RoomGameActionEntity()
                        {
                            RoomId = roomId,
                            PlayerId = action.PlayerId,
                            AffectedPlayerId = action.PlayerId.Value,
                            Action = ActionType.Suicide,
                            Night = room.CurrentNight + 1,
                            State = ActionState.Queued
                        };
                        actionsQueuedForNextNight.Add(vigilanteSuicideAction);
                    }

                    break;
                }
                case ActionType.Revive:
                {
                    if (playersKilledSet.Contains(action.AffectedPlayerId))
                    {
                        playersKilledSet.Remove(action.AffectedPlayerId);
                    }
                    playersRevivedSet.Add(action.AffectedPlayerId);
                    break;
                }
                default:
                    continue;
            }
        }
        //Set killed players as dead
        foreach (var player in playersDeadSet)
        {
            playersKilledSet.Add(player);
        }
        playerRoomRepository.UpdatePlayerIsAliveStatus(playersKilledSet.ToList(), false);
        roomGameActionRepository.MarkActionsAsProcessed(roomId, queuedActions);
        foreach (var roomGameActionEntity in actionsQueuedForNextNight)
        {
            roomGameActionRepository.QueueActionForPlayer(roomGameActionEntity);
        }
    }

    public void EndNight(string roomId)
    {
        ProcessQueuedActions(roomId);
        ProgressToNextPoint(roomId);
    }

    public void LynchChosenPlayer(string roomId, Guid? playerId)
    {
        if (!playerId.HasValue)
        {
            return;
        }
        var playerIdVal = playerId.Value;
        var player = playerRoomRepository.GetPlayerInRoom(roomId, playerIdVal);
        var room = roomRepository.GetRoom(roomId);
        var votedOutAction = new RoomGameActionEntity()
        {
            RoomId = roomId,
            PlayerId = playerIdVal,
            AffectedPlayerId = playerIdVal,
            Action = ActionType.VotedOut,
            State = ActionState.Processed,
            Night = room.CurrentNight
        };
        player.isAlive = false;
        playerRoomRepository.UpdatePlayerInRoom(player);
        roomGameActionRepository.QueueActionForPlayer(votedOutAction);
    }
    
    

    private void ResetRoomForNewGame(string roomId)
    {
        roomGameActionRepository.ClearAllActionsForRoom(roomId);
        var room = roomRepository.GetRoom(roomId);
        room.CurrentNight = 0;
        room.isDay = false;
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

    public RoleName? GetAssignedPlayerRole(string roomId, Guid playerId)
    {
        var playerInRoom = playerRoomRepository.GetPlayerInRoom(roomId, playerId);
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
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var allPlayersInGame = playerRoomRepository.GetPlayersInRoomWithARole(roomId);
        var playerRole = playerDetails.AssignedRole;
        if (!playerRole.HasValue)
        {
            throw new Exception("No role assigned");
        }

        var actionCheckDto = new ActionCheckDto()
        {
            CurrentPlayer = playerDetails,
            ProcessedActions = priorActions,
            QueuedActions = queuedActions,
            ActivePlayers = allPlayersInGame,
        };

        var role = roleFactory.GetRole(playerRole.Value);
        return role.GetActions(actionCheckDto);
    }

    public List<PlayerRoleActionDto> GetAllAssignedPlayerRolesAndActions(string roomId)
    {
        var currentMod = roomRepository.GetModeratorForRoom(roomId);
        var allPlayersInGame = playerRoomRepository.GetPlayersInRoomWithARole(roomId);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var roleActionList = new List<PlayerRoleActionDto>();

        foreach (var player in allPlayersInGame)
        {
            var actionCheckDto = new ActionCheckDto()
            {
                CurrentPlayer = player,
                ProcessedActions = priorActions,
                QueuedActions = queuedActions,
                ActivePlayers = allPlayersInGame,
            };
            var role = roleFactory.GetRole(player.AssignedRole!.Value);
            roleActionList.Add(
                new PlayerRoleActionDto()
                {
                    Id = player.PlayerGuid,
                    Nickname = player.NickName,
                    AvatarIndex = player.AvatarIndex,
                    Role = player.AssignedRole.Value,
                    Actions = role.GetActions(actionCheckDto),
                    isAlive = player.isAlive,
                });
        }

        return roleActionList;
    }

    public PlayerQueuedActionDTO? GetPlayerQueuedAction(string roomId, Guid playerId)
    {
        var queuedAction = roomGameActionRepository.GetQueuedPlayerActionForRoom(roomId, playerId);
        if (queuedAction == null)
        {
            return null;
        }

        var mappedAction = mapper.Map<PlayerQueuedActionDTO>(queuedAction);
        // var playerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.PlayerId).NickName;
        // var affectedPlayerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.AffectedPlayerId).NickName;
        // mappedAction.PlayerNickname = playerNickname;
        // mappedAction.AffectedPlayerNickname = affectedPlayerNickname;
        return mappedAction;
    }
    public List<PlayerQueuedActionDTO> GetAllQueuedActionsForRoom(string roomId)
    {
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        //Remove player unmmodifiable actions
        queuedActions.RemoveAll(queuedAction => queuedAction.Action.Equals(ActionType.Suicide));
        var mappedAction = mapper.Map<List<PlayerQueuedActionDTO>>(queuedActions);
        // var playerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.PlayerId).NickName;
        // var affectedPlayerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.AffectedPlayerId).NickName;
        // mappedAction.PlayerNickname = playerNickname;
        // mappedAction.AffectedPlayerNickname = affectedPlayerNickname;
        return mappedAction;
    }
    

    public void QueueActionForPlayer(PlayerActionRequestDTO playerActionRequestDto)
    {
        var night = roomRepository.GetRoom(playerActionRequestDto.RoomId).CurrentNight;
        RoomGameActionEntity? existingPlayerAction;
        if (playerActionRequestDto.Action == ActionType.WerewolfKill)
        {
            existingPlayerAction = roomGameActionRepository.GetQueuedWerewolfActionForRoom(playerActionRequestDto.RoomId);
        }
        else
        {
            if (!playerActionRequestDto.PlayerId.HasValue)
            {
                throw new Exception("No player assigned for this action");
            }
            existingPlayerAction = roomGameActionRepository.GetQueuedPlayerActionForRoom(
                playerActionRequestDto.RoomId, playerActionRequestDto.PlayerId.Value);
        }

        if (existingPlayerAction != null)
        {
            existingPlayerAction.Action = playerActionRequestDto.Action;
            existingPlayerAction.AffectedPlayerId = playerActionRequestDto.AffectedPlayerId;
            existingPlayerAction.Night = night;
            roomGameActionRepository.QueueActionForPlayer(existingPlayerAction);
        }
        else 
        {
            var playerAction = new RoomGameActionEntity()
            {
                Id = 0,
                RoomId = playerActionRequestDto.RoomId,
                PlayerId = playerActionRequestDto.PlayerId,
                Action = playerActionRequestDto.Action,
                AffectedPlayerId = playerActionRequestDto.AffectedPlayerId,
                State = ActionState.Queued,
                Night = night
            };
            roomGameActionRepository.QueueActionForPlayer(playerAction);
        }
    }

    public void DequeueActionForPlayer(int actionId)
    {
        roomGameActionRepository.DequeueActionForPlayer(
            actionId);
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

    public DayDto GetCurrentNightAndTime(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        return new DayDto()
        {
            CurrentNight = room.CurrentNight,
            IsDay = room.isDay,
        };
    }

    public void ProgressToNextPoint(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        if (room.isDay)
        {
            room.CurrentNight++;
            room.isDay = false;
        }
        else
        {
            room.isDay = true;
        }
        roomRepository.UpdateRoom(room);
        
    }
}