using AutoMapper;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Exceptions;
using WerewolfParty_Server.Models.Request;
using WerewolfParty_Server.Repository;

namespace WerewolfParty_Server.Service;

public class RoomService(
    RoomRepository roomRepository,
    PlayerRoomRepository playerRoomRepository,
    RoleSettingsRepository roleSettingsRepository,
    IMapper mapper)
{
    private const string allowedRoomIdCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    private const int roomIdLength = 5;

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
        var player = playerRoomRepository.GetPlayerInRoom(roomId, playerId);
        return mapper.Map<PlayerDTO>(player);
    }

    public bool isPlayerInRoom(string roomId, Guid playerId)
    {
        return playerRoomRepository.IsPlayerInRoom(playerId, roomId);
    }

    public RoleSettingsEntity GetRoleSettingsForRoom(string roomId)
    {
        return roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
    }

    public RoleSettingsEntity UpdateRoleSettingsForRoom(UpdateRoleSettingsRequest updateRoleSettingsRequest)
    {
        var oldRoleSettings = roleSettingsRepository.GetRoomSettingsById(updateRoleSettingsRequest.Id);
        oldRoleSettings.Werewolves = updateRoleSettingsRequest.Werewolves;
        oldRoleSettings.SelectedRoles = updateRoleSettingsRequest.SelectedRoles;
        return roleSettingsRepository.UpdateRoleSettings(oldRoleSettings);
    }


    public List<PlayerDTO> GetAllPlayersInRoom(string roomId, bool includeModerator = true)
    {
        var players = playerRoomRepository.GetPlayersInRoom(roomId);
        if (includeModerator) return mapper.Map<List<PlayerDTO>>(players);
        var room = roomRepository.GetRoom(roomId);
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
            CurrentModerator = playerGuid,
            CurrentNight = 0,
            isDay = false
        };
        roomRepository.CreateRoom(newRoom);

        //Set Default Role Settings For Room
        var DefaultRoleSettings = new RoleSettingsEntity()
        {
            RoomId = newRoomId,
            Werewolves = NumberOfWerewolves.One,
            SelectedRoles = [RoleName.Doctor, RoleName.Detective, RoleName.Witch]
        };
        roleSettingsRepository.AddRoleSettings(DefaultRoleSettings);

        return newRoomId;
    }

    public bool DoesRoomExist(string roomId)
    {
        return roomRepository.DoesRoomExist(roomId);
    }

    public void AddPlayerToRoom(string roomId, Guid playerId, AddEditPlayerDetailsDTO player)
    {
        var isPlayerAlreadyInRoom = playerRoomRepository.IsPlayerInRoom(playerId, roomId);
        if (isPlayerAlreadyInRoom)
            //Do nothing, player already connected
            return;
        var playerAdded = playerRoomRepository.AddPlayerToRoom(roomId, playerId, player);
    }

    public PlayerDTO GetModeratorForRoom(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }

        var mod = room.CurrentModerator;


        var moderatorDetails = playerRoomRepository.GetPlayerInRoom(roomId, mod);
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
        AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var player = playerRoomRepository.GetPlayerInRoom(roomId, playerId);
        if (player == null)
        {
            throw new PlayerNotFoundException($"Player with id {playerId} does not exist");
        }

        player.NickName = addEditPlayerDetails.NickName;
        player.AvatarIndex = addEditPlayerDetails.AvatarIndex;
        var updatedPlayer = playerRoomRepository.UpdatePlayerInRoom(roomId, player.PlayerGuid, player);
        return mapper.Map<PlayerDTO>(updatedPlayer);
    }

    public void RemovePlayerFromRoom(string roomId, Guid playerId)
    {
        playerRoomRepository.RemovePlayerFromRoom(roomId, playerId);
        //Replace Mod if player was mod
        var room = roomRepository.GetRoom(roomId);
        var otherPlayers = playerRoomRepository.GetPlayersInRoom(roomId);
        var newModerator = otherPlayers.FirstOrDefault()?.PlayerGuid;
        if (room.CurrentModerator == playerId)
        {
            room.CurrentModerator = newModerator ?? playerId;
        }

        roomRepository.UpdateRoom(room);
    }

    public void UpdateRoomGameState(string roomId, GameState gameState)
    {
        var room = roomRepository.GetRoom(roomId);
        room.GameState = gameState;
        roomRepository.UpdateRoom(room);
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