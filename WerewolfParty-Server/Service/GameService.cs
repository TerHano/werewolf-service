using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
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
        var queuedActions = await roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var room = await roomRepository.GetRoom(roomId);
        
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
        await roomGameActionRepository.MarkActionsAsProcessed(roomId, queuedActions);
        foreach (var roomGameActionEntity in actionsQueuedForNextNight)
        {
            await roomGameActionRepository.QueueActionForPlayer(roomGameActionEntity);
        }
    }

    public async Task EndNight(string roomId)
    {
        await ProcessQueuedActions(roomId);
        await ProgressToNextPoint(roomId);
    }

    public async Task LynchChosenPlayer(string roomId, int? playerId)
    {
        if (playerId.HasValue)
        {
            var playerIdVal = playerId.Value;
            var player =await  playerRoleRepository.GetPlayerRoleInRoom(roomId, playerIdVal);
            var room = await roomRepository.GetRoom(roomId);
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
            await roomGameActionRepository.QueueActionForPlayer(votedOutAction);
            await playerRoleRepository.UpdatePlayerRoleInRoom(player);
        }

        await ProgressToNextPoint(roomId);
    }


    private async Task ResetRoomForNewGame(string roomId)
    {
        await roomGameActionRepository.ClearAllActionsForRoom(roomId);
        var room = await roomRepository.GetRoom(roomId);
        room.CurrentNight = 0;
        room.isDay = false;
        room.WinCondition = WinCondition.None;
        await roomRepository.UpdateRoom(room);
        await playerRoleRepository.RemoveAllPlayerRolesForRoom(roomId);
    }

    private async Task<bool> IsEnoughPlayersForGame(string roomId)
    {
        var playersInLobby = await playerRoomRepository.GetPlayersInRoom(roomId);
        var roleSettingsForRoom = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        var playerCountWithoutMod = playersInLobby.Count - 1;
        var playersNeededForGame = roleSettingsForRoom.SelectedRoles.Count + roleSettingsForRoom.NumberOfWerewolves;
        return playerCountWithoutMod >= playersNeededForGame;
    }

    public async Task StartGame(string roomId)
    {
        var canStartGame = await IsEnoughPlayersForGame(roomId);
        if (!canStartGame)
        {
            throw new NotEnoughPlayersException("More players are needed for current game settings");
        }

        await ResetRoomForNewGame(roomId);
        await ShuffleAndAssignRoles(roomId);
        var room = await roomRepository.GetRoom(roomId);
        room.GameState = GameState.CardsDealt;
        await roomRepository.UpdateRoom(room);
    }

    public async Task<RoleName?> GetAssignedPlayerRole(string roomId, Guid playerGuid)
    {
        var doesPlayerHaveRole = await playerRoleRepository.DoesPlayerHaveRoleInRoom(roomId, playerGuid);
        if (!doesPlayerHaveRole) return null;
        var playerInRoom = await playerRoleRepository.GetPlayerRoleInRoomUsingPlayerGuid(roomId, playerGuid);
        return playerInRoom.Role;
    }
    // public List<PlayerRoleDTO> GetAllAssignedPlayerRolesAndActions(string roomId)
    // {
    //     var currentModerator = roomRepository.GetRoom(roomId).CurrentModerator;
    //     var playersInRoom = playerRoomRepository.GetPlayersInRoom(roomId);
    //     var playersInRoomWithoutMod = playersInRoom.Where(p => p.PlayerGuid != currentModerator).ToList();
    //     return mapper.Map<List<PlayerRoleDTO>>(playersInRoomWithoutMod);
    // }

    public async Task<List<RoleActionDto>> GetActionsForPlayerRole(string roomId, int playerRoleId)
    {
        var playerDetails = await playerRoleRepository.GetPlayerRoleInRoom(roomId, playerRoleId);
        var priorActions = await roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = await roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var allPlayersInGame = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        
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

    public async Task<List<PlayerRoleActionDto>> GetAllAssignedPlayerRolesAndActions(string roomId)
    {
        var allPlayerRolesInGame = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var priorActions = await roomGameActionRepository.GetAllProcessedActionsForRoom(roomId);
        var queuedActions = await roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        
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

    public async Task<PlayerQueuedActionDTO?> GetPlayerQueuedAction(string roomId, int playerRoleId)
    {
        var queuedAction = await roomGameActionRepository.GetQueuedPlayerActionForRoom(roomId, playerRoleId);
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

    public async Task<List<PlayerQueuedActionDTO>> GetAllQueuedActionsForRoom(string roomId)
    {
        var queuedActions = await roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        queuedActions.RemoveAll(queuedAction => queuedAction.Action.Equals(ActionType.Suicide));
        var mappedAction = mapper.Map<List<PlayerQueuedActionDTO>>(queuedActions);
        // var playerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.PlayerId).NickName;
        // var affectedPlayerNickname = playerRoomRepository.GetPlayerInRoom(roomId, mappedAction.AffectedPlayerRoleId).NickName;
        // mappedAction.PlayerNickname = playerNickname;
        // mappedAction.AffectedPlayerNickname = affectedPlayerNickname;
        return mappedAction;
    }


    public async Task QueueActionForPlayer(PlayerActionRequestDTO playerActionRequestDto)
    {
        var room = await roomRepository.GetRoom(playerActionRequestDto.RoomId);
        var night = room.CurrentNight;
        RoomGameActionEntity? existingPlayerAction;
        if (playerActionRequestDto.Action == ActionType.WerewolfKill)
        {
            existingPlayerAction =
                await roomGameActionRepository.GetQueuedWerewolfActionForRoom(playerActionRequestDto.RoomId);
        }
        else
        {
            if (!playerActionRequestDto.PlayerRoleId.HasValue)
            {
                throw new Exception("No player assigned for this action");
            }

            existingPlayerAction = await roomGameActionRepository.GetQueuedPlayerActionForRoom(
                playerActionRequestDto.RoomId, playerActionRequestDto.PlayerRoleId.Value);
        }

        if (existingPlayerAction != null)
        {
            existingPlayerAction.Action = playerActionRequestDto.Action;
            existingPlayerAction.AffectedPlayerRoleId = playerActionRequestDto.AffectedPlayerRoleId;
            existingPlayerAction.Night = night;
            await roomGameActionRepository.QueueActionForPlayer(existingPlayerAction);
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
            await roomGameActionRepository.QueueActionForPlayer(playerAction);
        }
    }

    public async Task DequeueActionForPlayer(int actionId)
    {
        await roomGameActionRepository.DequeueActionForPlayer(
            actionId);
    }

    public async Task<GameState> GetGameState(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        return room.GameState;
    }

    private async Task ShuffleAndAssignRoles(string roomId)
    {
        var roomModerator =  await roomRepository.GetModeratorForRoom(roomId);
        var playersInRoomWithoutMod = await playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, roomModerator);
        playersInRoomWithoutMod = playersInRoomWithoutMod.Shuffle();
        var roomSettings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

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

        await playerRoleRepository.AddPlayerRolesToRoom(playerRolesToAdd);
    }

    public async Task<DayDto> GetCurrentNightAndTime(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        return new DayDto()
        {
            CurrentNight = room.CurrentNight,
            IsDay = room.isDay,
        };
    }

    private async Task ProgressToNextPoint(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        if (room.isDay)
        {
            room.CurrentNight++;
            room.isDay = false;
        }
        else
        {
            room.isDay = true;
        }

        await roomRepository.UpdateRoom(room);
    }

    public async Task<List<PlayerDTO>> GetLatestDeaths(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        var currentNight = room.CurrentNight;
        var playersInGameTask = playerRoomRepository.GetPlayersInRoom(roomId);
        var gameDeathsTask = playerRoleRepository.GetPlayerRolesForRoom(roomId);
        await Task.WhenAll(playersInGameTask, gameDeathsTask);
        var playersInGame = await playersInGameTask;
        var gameDeaths = await gameDeathsTask;

        var playersDeadThisNight = playersInGame.Where((player) => gameDeaths
                .Any((x) => x.PlayerRoom.PlayerId == player.PlayerId && x.NightKilled == currentNight && !x.IsAlive))
            .ToList();
        return mapper.Map<List<PlayerDTO>>(playersDeadThisNight);
    }

    public async Task<WinCondition> CheckWinCondition(string roomId)
    {
        var winConditionForRoom = await roomRepository.GetWinConditionForRoom(roomId);
        if (winConditionForRoom != WinCondition.None)
        {
            return winConditionForRoom;
        }

        var playerRolesForRoom = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
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
            var room = await roomRepository.GetRoom(roomId);
            room.WinCondition = winCondition;
            await roomRepository.UpdateRoom(room);
        }

        return winCondition;
    }

    public async Task<WinCondition> GetWinConditionForRoom(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        return room.WinCondition;
    }
    
    //Create a method that returns a random description on how a player died, make it begin with were, make it environmental accidents
    

    public async Task<List<GameNightHistoryDTO>> GetGameSummary(string roomId)
    {
        var actions = await roomGameActionRepository.GetAllProcessedActionsForRoom(roomId, true);
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

        //Fill in days when no action was taken
        var maxNight = history.Count > 0 ?history.Max((e) => e.Night) : 0;
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
