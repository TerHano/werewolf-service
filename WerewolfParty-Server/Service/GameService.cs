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
        //var playersDeadSet = new HashSet<int>();
        var actionsQueuedForNextNight = new List<RoomGameActionEntity>();

        foreach (var action in queuedActions)
        {
            switch (action.Action)
            {
                case ActionType.Investigate:
                    break;
                case ActionType.Suicide:
                  //  playersDeadSet.Add(action.AffectedPlayerRoleId);
                  //  break;
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
                    var target = playerRoles.Find((player) =>
                        player.Id.Equals(action.AffectedPlayerRoleId));
                    if (target == null) throw new PlayerNotFoundException("Player not found");

                    // Guilt is decided by who the Vigilante shot, not by whether they survived:
                    // a Doctor or Witch saving the victim does not spare the Vigilante. This is
                    // checked before the revive test below so the outcome does not depend on the
                    // order actions come back from the database.
                    if (target.Role != RoleName.WereWolf)
                    {
                        if (!action.PlayerRoleId.HasValue)
                        {
                            throw new Exception("Vigilante action must have a player id");
                        }

                        //Vigilante will be set to be killed next night
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

                    //If the victim has been revived, they survive the shot (the Revive case also
                    //removes them from the killed set when it is processed after this one).
                    if (playersRevivedSet.Contains(action.AffectedPlayerRoleId)) break;
                    playersKilledSet.Add(action.AffectedPlayerRoleId);
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
        // foreach (var player in playersDeadSet)
        // {
        //     playersKilledSet.Add(player);
        // }

        await playerRoleRepository.UpdatePlayerStatusToDead(playersKilledSet.ToList(), room.CurrentNight);
        await roomGameActionRepository.MarkActionsAsProcessed(roomId, queuedActions);
        foreach (var roomGameActionEntity in actionsQueuedForNextNight)
        {
            await roomGameActionRepository.AddActionForPlayer(roomGameActionEntity);
        }
    }

    //Returns true if player is werewolf
    public async Task<InvestigatePlayerResult> InvestigatePlayerInRoom(InvestigatePlayerRequest request)
    {
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(request.RoomId);
        var player = playerRoles.FirstOrDefault(p=>p.Id.Equals(request.PlayerRoleId));
        if (player == null) throw new PlayerNotFoundException("Player not found");
        var playerRoleDto = mapper.Map<InvestigatedPlayerDTO>(player);
        bool isInvestigationCorrect;
        switch (request.InvestigationType)
        {
            case InvestigationType.Werewolf:
                isInvestigationCorrect = player.Role is RoleName.WereWolf or RoleName.Cursed;
                break;
            default:
                throw new Exception("Invalid investigation type");
        }
        return new InvestigatePlayerResult()
        {
            PlayerRole = playerRoleDto,
            IsInvestigationSuccessful = isInvestigationCorrect
        };
    }

    public async Task EndNight(string roomId)
    {
        await ProcessQueuedActions(roomId);
        await ProgressToNextPoint(roomId);
    }

    /// <summary>
    /// Hands the moderator badge to the first player eliminated, once per game.
    ///
    /// Being first out is boring, so the badge gives that person a job: they narrate the table
    /// and run the day. It carries no role information and no power over the night's timing —
    /// see the design doc for why a "skip step" control would leak.
    ///
    /// Returns the new badge holder, or null when nothing changed. Callers broadcast.
    /// </summary>
    public async Task<PlayerDTO?> AssignModeratorBadgeIfFirstDeath(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        if (room.ModeratorBadgeAssigned) return null;

        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);

        // Deaths resolve as a set — a werewolf kill and a Witch poison land together with the
        // same NightKilled — so "first" needs a tie-break. Lowest player role id: arbitrary,
        // deterministic, and not traceable to anything in the game state.
        var firstDead = playerRoles
            .Where(playerRole => !playerRole.IsAlive)
            .OrderBy(playerRole => playerRole.Id)
            .FirstOrDefault();

        if (firstDead == null) return null;

        room.CurrentModeratorId = firstDead.PlayerRoomId;
        room.ModeratorBadgeAssigned = true;
        await roomRepository.UpdateRoom(room);

        var newModerator = await playerRoomRepository.GetPlayerInRoom(roomId, firstDead.PlayerRoomId);
        return mapper.Map<PlayerDTO>(newModerator);
    }

    /// <summary>
    /// Adds another step's worth of time to the current night step, for a player who is
    /// visibly still fumbling with their phone.
    ///
    /// Extending is safe where shortening is not: it is a visible response to someone who has
    /// not finished, whereas ending a step early would make the length of the step a tell.
    /// </summary>
    public async Task<(bool Extended, DateTime? Deadline, NightStep? Step)> ExtendCurrentNightStep(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        // Nothing to extend outside a live step, and Resolving is not a step anyone acts in.
        if (room.NightStep == null || room.NightStep == NightStep.Resolving ||
            room.NightStepDeadline == null)
        {
            return (false, null, null);
        }

        var newDeadline = room.NightStepDeadline.Value.AddSeconds(settings.NightStepSeconds);

        // Guarded like every other write to these columns, so an extension cannot race the
        // clock advancing the step it was extending.
        var extended = await roomRepository.TryMoveToNightStep(roomId, room.NightStep, room.NightStep,
            newDeadline);

        return extended ? (true, newDeadline, room.NightStep) : (false, null, null);
    }

    /// <summary>
    /// Records the village's decision and moves the game on to the next night.
    ///
    /// Returns false when it is not day. <see cref="ProgressToNextPoint"/> simply toggles the
    /// phase, so a lynch arriving during the night would flip the room into day mid-night-call
    /// and leave the engine advancing steps for a phase that is no longer running.
    /// </summary>
    public async Task<bool> LynchChosenPlayer(string roomId, int? playerId)
    {
        var currentRoom = await roomRepository.GetRoom(roomId);
        if (!currentRoom.isDay) return false;

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
            await roomGameActionRepository.AddActionForPlayer(votedOutAction);
            await playerRoleRepository.UpdatePlayerRoleInRoom(player);
        }

        await ProgressToNextPoint(roomId);
        return true;
    }


    private async Task ResetRoomForNewGame(string roomId)
    {
        await roomGameActionRepository.ClearAllActionsForRoom(roomId);
        var room = await roomRepository.GetRoom(roomId);
        room.CurrentNight = 0;
        room.isDay = false;
        room.WinCondition = WinCondition.None;
        // A restart must not leave a half-finished night call running, or the clock would keep
        // advancing steps for a game that no longer exists.
        room.NightStep = null;
        room.NightStepDeadline = null;
        // A new game means a new first death, so the badge is up for grabs again.
        room.ModeratorBadgeAssigned = false;
        await roomRepository.UpdateRoom(room);
        await playerRoleRepository.RemoveAllPlayerRolesForRoom(roomId);
    }

    private async Task<bool> IsEnoughPlayersForGame(string roomId)
    {
        var playersInLobby = await playerRoomRepository.GetPlayersInRoom(roomId);
        var roleSettingsForRoom = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        // In a self-moderated room the moderator is dealt in like anyone else, so every player
        // in the lobby counts towards the deck. Otherwise the moderator sits the game out and
        // one seat has to be subtracted.
        var playersToDealTo = roleSettingsForRoom.SelfModerated
            ? playersInLobby.Count
            : playersInLobby.Count - 1;
        var playersNeededForGame = roleSettingsForRoom.SelectedRoles.Count + roleSettingsForRoom.NumberOfWerewolves;
        return playersToDealTo >= playersNeededForGame;
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

    /// <summary>
    /// The caller's own card, including the player role id every game action is addressed by.
    /// Returns null for someone in the room who was never dealt in.
    /// </summary>
    public async Task<MyRoleDto?> GetMyRole(string roomId, Guid playerGuid)
    {
        var doesPlayerHaveRole = await playerRoleRepository.DoesPlayerHaveRoleInRoom(roomId, playerGuid);
        if (!doesPlayerHaveRole) return null;

        var playerRole = await playerRoleRepository.GetPlayerRoleInRoomUsingPlayerGuid(roomId, playerGuid);
        return new MyRoleDto
        {
            PlayerRoleId = playerRole.Id,
            Role = playerRole.Role,
            IsAlive = playerRole.IsAlive,
            NightKilled = playerRole.NightKilled
        };
    }

    /// <summary>
    /// Everyone dealt into this game, by player role id, with no roles attached. This is what
    /// lets a player put names to the target ids their own action list gives them.
    /// </summary>
    public async Task<List<GamePlayerDto>> GetPlayersInGame(string roomId)
    {
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        return playerRoles
            .Select(playerRole => new GamePlayerDto
            {
                Id = playerRole.Id,
                Nickname = playerRole.PlayerRoom.NickName,
                AvatarIndex = playerRole.PlayerRoom.AvatarIndex,
                IsAlive = playerRole.IsAlive
            })
            .ToList();
    }

    /// <summary>
    /// The werewolves in this game, alive or dead. Only ever served to a werewolf — the pack
    /// has to recognise each other to share a single kill.
    /// </summary>
    public async Task<List<PlayerRoleDTO>> GetWerewolfPack(string roomId)
    {
        var playerRoles = await playerRoleRepository.GetPlayerRolesForRoom(roomId);
        var pack = playerRoles.Where(player => player.Role == RoleName.WereWolf).ToList();
        return mapper.Map<List<PlayerRoleDTO>>(pack);
    }

    public async Task<PlayerQueuedActionDTO?> GetPlayerQueuedAction(string roomId, int playerRoleId)
    {
        var queuedAction = await roomGameActionRepository.GetQueuedPlayerActionForRoom(roomId, playerRoleId);
        if (queuedAction == null)
        {
            return null;
        }

        var mappedAction = mapper.Map<PlayerQueuedActionDTO>(queuedAction);

        return mappedAction;
    }

    public async Task<List<PlayerQueuedActionDTO>> GetAllQueuedActionsForRoom(string roomId)
    {
        var queuedActions = await roomGameActionRepository.GetAllQueuedActionsForRoom(roomId);
        queuedActions.RemoveAll(queuedAction => queuedAction.Action.Equals(ActionType.Suicide));
        var mappedAction = mapper.Map<List<PlayerQueuedActionDTO>>(queuedActions);
  
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
            await roomGameActionRepository.AddActionForPlayer(existingPlayerAction);
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
            await roomGameActionRepository.AddActionForPlayer(playerAction);
        }
    }

    /// <summary>
    /// Returns the room an action belongs to, or null if the action does not exist. Used to
    /// scope authorization for routes that only carry an action id.
    /// </summary>
    public async Task<string?> GetRoomIdForAction(int actionId)
    {
        var action = await roomGameActionRepository.GetActionById(actionId);
        return action?.RoomId;
    }

    public async Task DequeueActionForPlayer(int actionId)
    {
        await roomGameActionRepository.RemoveActionForPlayer(
            actionId);
    }

    public async Task<GameState> GetGameState(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        return room.GameState;
    }

    private async Task ShuffleAndAssignRoles(string roomId)
    {
        var roomSettings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);

        // A self-moderated room has no human calling the night, so the moderator is a normal
        // player and gets a card. In a moderator-run room they are excluded from the deal.
        List<PlayerRoomEntity> playersToDealTo;
        if (roomSettings.SelfModerated)
        {
            playersToDealTo = await playerRoomRepository.GetPlayersInRoom(roomId);
        }
        else
        {
            var roomModerator = await roomRepository.GetModeratorForRoom(roomId);
            playersToDealTo = await playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, roomModerator);
        }

        var roleCards = new List<RoleName>(roomSettings.SelectedRoles);
        for (int i = 0; i < roomSettings.NumberOfWerewolves; i++)
        {
            roleCards.Add(RoleName.WereWolf);
        }

        playersToDealTo = playersToDealTo.Shuffle();
        var playerRolesToAdd = new List<PlayerRoleEntity>();
        for (int i = 0; i < playersToDealTo.Count; i++)
        {
            var player = playersToDealTo[i];
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
        // Sequential, not Task.WhenAll: both repositories share this scope's DbContext, which
        // is not thread-safe. Running them together throws "A second operation was started on
        // this context instance", which surfaced as a 500 on the morning deaths banner and a
        // silent "nobody died" in the UI.
        var playersInGame = await playerRoomRepository.GetPlayersInRoom(roomId);
        var gameDeaths = await playerRoleRepository.GetPlayerRolesForRoom(roomId);

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
