using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Models.Request;
using WerewolfParty_Server.Repository;

namespace WerewolfParty_Server.Service;

public class RoomService(
    RoomRepository roomRepository,
    PlayerRoomRepository playerRoomRepository,
    RoleSettingsRepository roleSettingsRepository,
    IMapper mapper)
{
    private readonly string allowedRoomIdCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    private readonly int roomIdLength = 5;

    public List<RoomEntity> GetAllRooms()
    {
        return roomRepository.GetAllRooms();
    }

    public RoomEntity GetRoom(string roomId)
    {
        return roomRepository.GetRoom(roomId);
    }

    public PlayerDTO GetPlayerInRoom(string roomId, Guid playerId)
    {
        var sanitizedRoomId = roomId.ToUpper();
        var player = playerRoomRepository.GetPlayerInRoom(sanitizedRoomId, playerId);
        return mapper.Map<PlayerDTO>(player);
    }

    public bool isPlayerInRoom(string roomId, Guid playerId)
    {
        var sanitizedRoomId = roomId.ToUpper();
        return playerRoomRepository.IsPlayerInRoom(playerId, sanitizedRoomId);
    }

    public RoleSettingsEntity GetRoleSettingsForRoom(string roomId)
    {
        var sanitizedRoomId = roomId.ToUpper();
        return roleSettingsRepository.GetRoomSettingsByRoomId(sanitizedRoomId);
    }

    public RoleSettingsEntity UpdateRoleSettingsForRoom(UpdateRoleSettingsRequest updateRoleSettingsRequest)
    {
        var sanitizedRoomId = updateRoleSettingsRequest.RoomId.ToUpper();
        var oldRoleSettings = roleSettingsRepository.GetRoomSettingsById(updateRoleSettingsRequest.RoleSettingsId);
        oldRoleSettings.Werewolves = updateRoleSettingsRequest.Werewolves;
        oldRoleSettings.SelectedRoles = updateRoleSettingsRequest.SelectedRoles;
        return roleSettingsRepository.UpdateRoleSettings(oldRoleSettings);
    }


    public List<PlayerDTO> GetAllPlayersInRoom(string roomId, bool includeModerator = true)
    {
        var sanitizedRoomId = roomId.ToUpper();
        var players = playerRoomRepository.GetPlayersInRoom(sanitizedRoomId);
        if (includeModerator) return mapper.Map<List<PlayerDTO>>(players);
        var room = roomRepository.GetRoom(sanitizedRoomId);
        players.RemoveAll((player) => player.PlayerGuid == room.CurrentModerator);
        return mapper.Map<List<PlayerDTO>>(players);
    }

    public string CreateRoom(Guid playerGuid)
    {
        var newRoomId = GenerateRoomId();
        var newRoom = new RoomEntity
        {
            Id = newRoomId,
            GameState = GameState.Lobby,
            CurrentModerator = playerGuid
        };
        roomRepository.CreateRoom(newRoom);

        //Set Default Role Settings For Room
        var DefaultRoleSettings = new RoleSettingsEntity()
        {
            RoomId = newRoomId,
            Werewolves = NumberOfWerewolves.One,
            SelectedRoles = [RoleName.Doctor, RoleName.Seer, RoleName.Witch]
        };
        roleSettingsRepository.AddRoleSettings(DefaultRoleSettings);

        return newRoomId;
    }

    public bool DoesRoomExist(string roomId)
    {
        var sanitizedRoomId = roomId.ToUpper();
        return roomRepository.DoesRoomExist(sanitizedRoomId);
    }

    public void AddPlayerToRoom(string roomId, Guid playerId, AddUpdatePlayerDetailsDTO player)
    {
        var sanitizedRoomId = roomId.ToUpper();
        var isPlayerAlreadyInRoom = playerRoomRepository.IsPlayerInRoom(playerId, sanitizedRoomId);
        if (isPlayerAlreadyInRoom)
            //Do nothing, player already connected
            return;
        var playerAdded = playerRoomRepository.AddPlayerToRoom(sanitizedRoomId, playerId, player);
    }

    public PlayerDTO GetModeratorForRoom(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }

        var mod = room.CurrentModerator;
        if (!mod.HasValue)
        {
            throw new Exception("Issue getting moderator");
        }

        var moderatorDetails = playerRoomRepository.GetPlayerInRoom(roomId, mod.Value);
        return mapper.Map<PlayerDTO>(moderatorDetails);
    }

    public void UpdateModeratorForRoom(string roomId, Guid newModeratorplayerId)
    {
        var room = roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }

        room.CurrentModerator = newModeratorplayerId;
        roomRepository.UpdateRoom(room);
    }

    public PlayerDTO UpdatePlayerDetailsForRoom(string roomId, Guid playerId,
        AddUpdatePlayerDetailsDTO addUpdatePlayerDetails)
    {
        var sanitizedRoomId = roomId.ToUpper();
        var player = playerRoomRepository.GetPlayerInRoom(sanitizedRoomId, playerId);
        if (player == null)
        {
            throw new PlayerNotFoundException($"Player with id {playerId} does not exist");
        }

        player.NickName = addUpdatePlayerDetails.NickName;
        player.AvatarIndex = addUpdatePlayerDetails.AvatarIndex;
        var updatedPlayer = playerRoomRepository.UpdatePlayerInRoom(roomId, player.PlayerGuid, addUpdatePlayerDetails);
        return mapper.Map<PlayerDTO>(updatedPlayer);
    }

    public void RemovePlayerFromRoom(string roomId, Guid playerId)
    {
        var sanitizedRoomId = roomId.ToUpper();
        playerRoomRepository.RemovePlayerFromRoom(sanitizedRoomId, playerId);
    }

    public void UpdateRoomGameState(string roomId, GameState gameState)
    {
        var room = roomRepository.GetRoom(roomId);
        room.GameState = gameState;
        roomRepository.UpdateRoom(room);
    }

    public List<PlayerRoleDTO> ShuffleAndAssignRoles(string roomId)
    {
        var sanitizedRoomId = roomId.ToUpper();
        var playersInRoom = playerRoomRepository.GetPlayersInRoom(sanitizedRoomId);
        var roomSettings = roleSettingsRepository.GetRoomSettingsByRoomId(sanitizedRoomId);

        var roleCards = roomSettings.SelectedRoles;
        for (int i = 0; i < (int)roomSettings.Werewolves; i++)
        {
            roleCards.Add(RoleName.WereWolf);
        }

        var shuffledRoles = roleCards.Shuffle();
        for (int i = 0; i < playersInRoom.Count; i++)
        {
            var player = playersInRoom[i];
            if (i > roleCards.Count - 1)
            {
                player.AssignedRole = RoleName.Villager;
            }
            else
            {
                player.AssignedRole = shuffledRoles[i];
            }
        }

        var assignedRoles = playerRoomRepository.UpdateGroupOfPlayersInRoom(playersInRoom);

        return mapper.Map<List<PlayerRoleDTO>>(assignedRoles);
        ;
    }

    private string GenerateRoomId()
    {
        var random = new Random();
        var allowedRoomIdCharactersLength = allowedRoomIdCharacters.Length;
        var isUniqueRoomId = false;
        var generatedRoomId = string.Empty;
        while (isUniqueRoomId == false)
        {
            var chars = new char[roomIdLength];

            for (var i = 0; i < roomIdLength; i++)
                chars[i] = allowedRoomIdCharacters[random.Next(0, allowedRoomIdCharactersLength)];
            generatedRoomId = new string(chars);
            isUniqueRoomId = DoesRoomExist(generatedRoomId) == false;
        }

        return generatedRoomId;
    }
}