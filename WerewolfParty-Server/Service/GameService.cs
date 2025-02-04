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
    IMapper mapper)
{
    private async Task ProcessQueuedActions(string roomId)
    {
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var playerRoles = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var room = roomRepository.GetRoom(roomId);
        var playersRevivedSet = new HashSet<int>();
        var playersKilledSet = new HashSet<int>();
        var playersDeadSet = new HashSet<int>();
        var actionsQueuedForNextNight = new List<RoomGameActionEntity>();

        foreach (var action in queuedActions)
        {
            switch (action.Action)
            {
                case ActionType.Investigate:
                    break;
                case ActionType.Suicide:
                    playersDeadSet.Add(action.AffectedPlayerRoleId);
                    break;
                case ActionType.WerewolfKill:
                case ActionType.Kill:
                {
                    //If player has been revived, they cannot be killed this night
                    if (playersRevivedSet.Contains(action.AffectedPlayerRoleId)) continue;
                    playersKilledSet.Add(action.AffectedPlayerRoleId);
                    break;
                }
                case ActionType.VigilanteKill:
                {
                    if (playersRevivedSet.Contains(action.AffectedPlayerRoleId)) continue;
                    playersKilledSet.Add(action.AffectedPlayerRoleId);
                    var killedPlayer = playerRoles.Find((player) =>
                        player.Id.Equals(action.AffectedPlayerRoleId));
                    if (killedPlayer == null) throw new Exception("Player not found");
                    if (killedPlayer.Role != RoleName.WereWolf)
                    {
                        if (!action.PlayerRoleId.HasValue)
                        {
                            throw new Exception("Vigilante action must have a player id");
                        }

                        //Vigilante will be set to be killed next night;
                        var vigilanteSuicideAction = new RoomGameActionEntity()
                        {
                            RoomId = roomId,
                            PlayerRoleId = action.PlayerRoleId.Value,
                            AffectedPlayerRoleId = action.PlayerRoleId.Value,
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
                    if (playersKilledSet.Contains(action.AffectedPlayerRoleId))
                    {
                        playersKilledSet.Remove(action.AffectedPlayerRoleId);
                    }

                    playersRevivedSet.Add(action.AffectedPlayerRoleId);
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

        await playerRoleRepository.UpdatePlayerStatusToDead(playersKilledSet.ToList(), room.CurrentNight);
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

    public void LynchChosenPlayer(string roomId, int? playerId)
    {
        if (playerId.HasValue)
        {
            var playerIdVal = playerId.Value;
            var player = playerRoleRepository.GetPlayerRoleInRoom(roomId, playerIdVal);
            var room = roomRepository.GetRoom(roomId);
            var votedOutAction = new RoomGameActionEntity()
            {
                RoomId = roomId,
                PlayerRoleId = null,
                AffectedPlayerRoleId = playerIdVal,
                Action = ActionType.VotedOut,
                State = ActionState.Processed,
                Night = room.CurrentNight
            };
            player.IsAlive = false;
            player.NightKilled = room.CurrentNight;
            roomGameActionRepository.QueueActionForPlayer(votedOutAction);
            playerRoleRepository.UpdatePlayerRoleInRoom(player);
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

    public async Task StartGame(string roomId)
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
        await roomRepository.UpdateRoom(room);
    }

    public RoleName? GetAssignedPlayerRole(string roomId, Guid playerGuid)
    {
        var doesPlayerHaveRole = playerRoleRepository.DoesPlayerHaveRoleInRoom(roomId, playerGuid);
        if (!doesPlayerHaveRole) return null;
        var playerInRoom = playerRoleRepository.GetPlayerRoleInRoomUsingPlayerGuid(roomId, playerGuid);
        return playerInRoom.Role;
    }
    // public List<PlayerRoleDTO> GetAllAssignedPlayerRolesAndActions(string roomId)
    // {
    //     var currentModerator = roomRepository.GetRoom(roomId).CurrentModerator;
    //     var playersInRoom = playerRoomRepository.GetPlayersInRoom(roomId);
    //     var playersInRoomWithoutMod = playersInRoom.Where(p => p.PlayerGuid != currentModerator).ToList();
    //     return mapper.Map<List<PlayerRoleDTO>>(playersInRoomWithoutMod);
    // }

    public List<RoleActionDto> GetActionsForPlayerRole(string roomId, int playerRoleId)
    {
        var playerDetails = playerRoleRepository.GetPlayerRoleInRoom(roomId, playerRoleId);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var allPlayersInGame = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var settings = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        var playerRole = playerDetails.Role;

        var actionCheckDto = new ActionCheckDto()
        {
            CurrentPlayer = playerDetails,
            ProcessedActions = priorActions,
            QueuedActions = queuedActions,
            ActivePlayers = allPlayersInGame,
            Settings = settings,
        };

        var role = RoleFactory.GetRole(playerRole);
        return role.GetActions(actionCheckDto);
    }

    public List<PlayerRoleActionDto> GetAllAssignedPlayerRolesAndActions(string roomId)
    {
        var allPlayerRolesInGame = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var priorActions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var settings = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        var roleActionList = new List<PlayerRoleActionDto>();

        foreach (var playerRole in allPlayerRolesInGame)
        {
            var actionCheckDto = new ActionCheckDto()
            {
                CurrentPlayer = playerRole,
                ProcessedActions = priorActions,
                QueuedActions = queuedActions,
                ActivePlayers = allPlayerRolesInGame,
                Settings = settings,
            };
            var role = RoleFactory.GetRole(playerRole.Role);
            // var playerInfo = playerRoomRepository.GetPlayerInRoom(roomId, playerRole.PlayerId);
            roleActionList.Add(
                new PlayerRoleActionDto()
                {
                    Id = playerRole.Id,
                    Nickname = playerRole.PlayerRoom.NickName,
                    AvatarIndex = playerRole.PlayerRoom.AvatarIndex,
                    Role = playerRole.Role,
                    Actions = role.GetActions(actionCheckDto),
                    isAlive = playerRole.IsAlive,
                });
        }

        return roleActionList;
    }

    public PlayerQueuedActionDTO? GetPlayerQueuedAction(string roomId, int playerRoleId)
    {
        var queuedAction = roomGameActionRepository.GetQueuedPlayerActionForRoom(roomId, playerRoleId);
        if (queuedAction == null)
        {
            return null;
        }

        var mappedAction = mapper.Map<PlayerQueuedActionDTO>(queuedAction);
        // var playerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.PlayerId).NickName;
        // var affectedPlayerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.AffectedPlayerRoleId).NickName;
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
        // var affectedPlayerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.AffectedPlayerRoleId).NickName;
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
            if (!playerActionRequestDto.PlayerRoleId.HasValue)
            {
                throw new Exception("No player assigned for this action");
            }

            existingPlayerAction = roomGameActionRepository.GetQueuedPlayerActionForRoom(
                playerActionRequestDto.RoomId, playerActionRequestDto.PlayerRoleId.Value);
        }

        if (existingPlayerAction != null)
        {
            existingPlayerAction.Action = playerActionRequestDto.Action;
            existingPlayerAction.AffectedPlayerRoleId = playerActionRequestDto.AffectedPlayerRoleId;
            existingPlayerAction.Night = night;
            roomGameActionRepository.QueueActionForPlayer(existingPlayerAction);
        }
        else
        {
            var playerAction = new RoomGameActionEntity()
            {
                Id = 0,
                RoomId = playerActionRequestDto.RoomId,
                PlayerRoleId = playerActionRequestDto.PlayerRoleId,
                Action = playerActionRequestDto.Action,
                AffectedPlayerRoleId = playerActionRequestDto.AffectedPlayerRoleId,
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

    private void ProgressToNextPoint(string roomId)
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

    public List<GameNightHistoryDTO> GetGameSummary(string roomId)
    {
        var actions = roomGameActionRepository.GetAllProcessedActionsForRoom(roomId, true);
        var history = actions.GroupBy((e) => e.Night).OrderBy(e => e.Key)
            .Select(e =>
            {
                return new GameNightHistoryDTO()
                {
                    Night = e.Key,
                    NightActions = mapper.Map<List<PlayerGameActionDTO>>(e.Where(x => x.Action != ActionType.VotedOut)
                        .Select(x => new PlayerGameActionDTO()
                        {
                            Id = x.Id,
                            Player = mapper.Map<PlayerRoleDTO>(x.PlayerRole),
                            Action = x.Action,
                            AffectedPlayer = mapper.Map<PlayerRoleDTO>(x.AffectedPlayerRole),
                        }).ToList()),
                    DayActions = mapper.Map<List<PlayerGameActionDTO>>(e.Where(x => x.Action == ActionType.VotedOut)
                        .Select(x => new PlayerGameActionDTO()
                        {
                            Id = x.Id,
                            Player = mapper.Map<PlayerRoleDTO>(x.PlayerRole),
                            Action = x.Action,
                            AffectedPlayer = mapper.Map<PlayerRoleDTO>(x.AffectedPlayerRole),
                        }).ToList()),
                };
            }).ToList();

        //Fill in days where no action was taken
        var maxNight = history.Max((e) => e.Night);
        for (var i = 0; i < maxNight; i++)
        {
            if (!history.Exists((e) => e.Night == i))
            {
                history.Insert(i, new GameNightHistoryDTO()
                {
                    Night = i,
                    NightActions = [],
                    DayActions = []
                });
            }
        }

        return history;
    }
}