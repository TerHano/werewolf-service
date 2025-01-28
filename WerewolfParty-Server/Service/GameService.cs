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
    PlayerRoleRepository playerRoleRepository,
    RoomRepository roomRepository,
    RoleSettingsRepository roleSettingsRepository,
    RoleFactory roleFactory,
    IMapper mapper)
{
    private void ProcessQueuedActions(string roomId)
    {
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var playerRoles = playerRoleRepository.GetPlayerRolesForRoom(roomId);
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
                    var killedPlayer = playerRoles.Find((player) =>
                        player.PlayerRoom.PlayerId.Equals(action.AffectedPlayerId));
                    if (killedPlayer == null) throw new Exception("Player not found");
                    if (killedPlayer.Role != RoleName.WereWolf)
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

        playerRoleRepository.UpdatePlayerStatusToDead(playersKilledSet.ToList(), room.CurrentNight);
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
        if (playerId.HasValue)
        {
            var playerIdVal = playerId.Value;
            var player = playerRoleRepository.GetPlayerRoleInRoom(roomId, playerIdVal);
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
            player.IsAlive = false;
            player.NightKilled = room.CurrentNight;
            player.WasVotedOut = true;
            playerRoleRepository.UpdatePlayerRoleInRoom(player);
            roomGameActionRepository.QueueActionForPlayer(votedOutAction);
        }

        ProgressToNextPoint(roomId);
    }


    private void ResetRoomForNewGame(string roomId)
    {
        roomGameActionRepository.ClearAllActionsForRoom(roomId);
        var room = roomRepository.GetRoom(roomId);
        room.CurrentNight = 0;
        room.isDay = false;
        room.WinCondition = WinCondition.None;
        roomRepository.UpdateRoom(room);
        playerRoleRepository.RemoveAllPlayerRolesForRoom(roomId);
    }

    private bool IsEnoughPlayersForGame(string roomId)
    {
        var playersInLobby = playerRoomRepository.GetPlayersInRoom(roomId);
        var roleSettingsForRoom = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        var playerCountWithoutMod = playersInLobby.Count - 1;
        var playersNeededForGame = roleSettingsForRoom.SelectedRoles.Count + roleSettingsForRoom.NumberOfWerewolves;
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
        var doesPlayerHaveRole = playerRoleRepository.DoesPlayerHaveRoleInRoom(roomId, playerId);
        if (!doesPlayerHaveRole) return null;
        var playerInRoom = playerRoleRepository.GetPlayerRoleInRoom(roomId, playerId);
        return playerInRoom.Role;
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
        var playerDetails = playerRoleRepository.GetPlayerRoleInRoom(roomId, playerId);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var allPlayersInGame = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var playerRole = playerDetails.Role;

        var actionCheckDto = new ActionCheckDto()
        {
            CurrentPlayer = playerDetails,
            ProcessedActions = priorActions,
            QueuedActions = queuedActions,
            ActivePlayers = allPlayersInGame,
        };

        var role = roleFactory.GetRole(playerRole);
        return role.GetActions(actionCheckDto);
    }

    public List<PlayerRoleActionDto> GetAllAssignedPlayerRolesAndActions(string roomId)
    {
        var currentMod = roomRepository.GetModeratorForRoom(roomId);
        var allPlayerRolesInGame = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var roleActionList = new List<PlayerRoleActionDto>();

        foreach (var playerRole in allPlayerRolesInGame)
        {
            var actionCheckDto = new ActionCheckDto()
            {
                CurrentPlayer = playerRole,
                ProcessedActions = priorActions,
                QueuedActions = queuedActions,
                ActivePlayers = allPlayerRolesInGame,
            };
            var role = roleFactory.GetRole(playerRole.Role);
            // var playerInfo = playerRoomRepository.GetPlayerInRoom(roomId, playerRole.PlayerId);
            roleActionList.Add(
                new PlayerRoleActionDto()
                {
                    Id = playerRole.PlayerRoom.PlayerId,
                    Nickname = playerRole.PlayerRoom.NickName,
                    AvatarIndex = playerRole.PlayerRoom.AvatarIndex,
                    Role = playerRole.Role,
                    Actions = role.GetActions(actionCheckDto),
                    isAlive = playerRole.IsAlive,
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
            existingPlayerAction =
                roomGameActionRepository.GetQueuedWerewolfActionForRoom(playerActionRequestDto.RoomId);
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

    private void ShuffleAndAssignRoles(string roomId)
    {
        var roomModerator = roomRepository.GetModeratorForRoom(roomId);
        var playersInRoomWithoutMod = playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, roomModerator);
        playersInRoomWithoutMod = playersInRoomWithoutMod.Shuffle();
        var roomSettings = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        var roleCards = new List<RoleName>(roomSettings.SelectedRoles);
        for (int i = 0; i < roomSettings.NumberOfWerewolves; i++)
        {
            roleCards.Add(RoleName.WereWolf);
        }

        playersInRoomWithoutMod = playersInRoomWithoutMod.Shuffle();
        var playerRolesToAdd = new List<PlayerRoleEntity>();
        for (int i = 0; i < playersInRoomWithoutMod.Count; i++)
        {
            var player = playersInRoomWithoutMod[i];
            var role = i > roleCards.Count - 1 ? RoleName.Villager : roleCards[i];

            var newPlayerRole = new PlayerRoleEntity()
            {
                RoomId = roomId,
                PlayerRoomId = player.Id,
                IsAlive = true,
                Role = role,
                WasVotedOut = false
            };
            playerRolesToAdd.Add(newPlayerRole);
        }

        playerRoleRepository.AddPlayerRolesToRoom(playerRolesToAdd);
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

    public List<PlayerDTO> GetLatestDeaths(string roomId)
    {
        var currentNight = roomRepository.GetRoom(roomId).CurrentNight;
        var playersInGame = playerRoomRepository.GetPlayersInRoom(roomId);
        var gameDeaths = playerRoleRepository.GetPlayerRolesForRoom(roomId);

        var playersDeadThisNight = playersInGame.Where((player) => gameDeaths
                .Any((x) => x.PlayerRoom.PlayerId == player.PlayerId && x.NightKilled == currentNight && !x.IsAlive))
            .ToList();
        return mapper.Map<List<PlayerDTO>>(playersDeadThisNight);
    }

    public WinCondition CheckWinCondition(string roomId)
    {
        var winConditionForRoom = roomRepository.GetWinConditionForRoom(roomId);
        if (winConditionForRoom != WinCondition.None)
        {
            return winConditionForRoom;
        }

        var playerRolesForRoom = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var aliveWerewolvesCount =
            playerRolesForRoom.Count(player => player is { IsAlive: true, Role: RoleName.WereWolf });
        var otherPlayersCount = playerRolesForRoom.Count(player => player.IsAlive && player.Role != RoleName.WereWolf);
        WinCondition winCondition = WinCondition.None;
        if (aliveWerewolvesCount.Equals(0))
        {
            winCondition = WinCondition.Villagers;
        }

        if (aliveWerewolvesCount >= otherPlayersCount)
        {
            winCondition = WinCondition.Werewolves;
        }

        if (winCondition != WinCondition.None)
        {
            var room = roomRepository.GetRoom(roomId);
            room.WinCondition = winCondition;
            roomRepository.UpdateRoom(room);
        }

        return winCondition;
    }

    public WinCondition GetWinConditionForRoom(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        return room.WinCondition;
    }
}