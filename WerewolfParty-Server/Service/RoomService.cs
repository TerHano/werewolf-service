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

    public PlayerDTO GetPlayerInRoomUsingGuid(string roomId, Guid playerId)
    {
        var player = playerRoomRepository.GetPlayerInRoomUsingPlayerGuid(roomId, playerId);
        return mapper.Map<PlayerDTO>(player);
    }

    public PlayerDTO GetPlayerInRoom(string roomId, int playerId)
    {
        var player = playerRoomRepository.GetPlayerInRoom(roomId, playerId);
        return mapper.Map<PlayerDTO>(player);
    }

    public bool isPlayerInRoom(string roomId, Guid playerId)
    {
        return playerRoomRepository.IsPlayerInRoom(playerId, roomId);
    }

    public RoomSettingsDto GetRoleSettingsForRoom(string roomId)
    {
        var settings = roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        return mapper.Map<RoomSettingsDto>(settings);
    }

    public async Task UpdateRoleSettingsForRoom(UpdateRoleSettingsRequest updateRoleSettingsRequest)
    {
        var oldRoleSettings = roleSettingsRepository.GetRoomSettingsById(updateRoleSettingsRequest.Id);
        oldRoleSettings.NumberOfWerewolves = updateRoleSettingsRequest.NumberOfWerewolves;
        oldRoleSettings.SelectedRoles = updateRoleSettingsRequest.SelectedRoles;
        oldRoleSettings.ShowGameSummary = updateRoleSettingsRequest.ShowGameSummary;
        oldRoleSettings.AllowMultipleSelfHeals = updateRoleSettingsRequest.AllowMultipleSelfHeals;
        await roleSettingsRepository.UpdateRoleSettings(oldRoleSettings);
    }


    public List<PlayerDTO> GetAllPlayersInRoom(string roomId, Guid? playerId = null, bool includeModerator = true)
    {
        var room = roomRepository.GetRoom(roomId);
        var players = includeModerator
            ? playerRoomRepository.GetPlayersInRoom(roomId)
            : playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, room.CurrentModeratorId);
        if (!playerId.HasValue) return mapper.Map<List<PlayerDTO>>(players);
        var currentPlayer = players.FirstOrDefault((player) => player.PlayerId.Equals(playerId.Value));
        if (currentPlayer == null) return mapper.Map<List<PlayerDTO>>(players);
        players.Remove(currentPlayer);
        players.Insert(0, currentPlayer);
        return mapper.Map<List<PlayerDTO>>(players);
    }

    public async Task<string> CreateRoom()
    {
        var newRoomId = GenerateRoomId();
        var newRoom = new RoomEntity
        {
            Id = newRoomId,
            GameState = GameState.Lobby,
            CurrentModeratorId = null,
            CurrentNight = 0,
            isDay = false,
            WinCondition = WinCondition.None,
            LastModifiedDate = DateTime.UtcNow,
        };
        await roomRepository.CreateRoom(newRoom);

        //Set Default Role Settings For Room
        var DefaultRoleSettings = new RoomSettingsEntity()
        {
            RoomId = newRoomId,
            NumberOfWerewolves = 1,
            SelectedRoles = [RoleName.Doctor, RoleName.Detective, RoleName.Witch],
            ShowGameSummary = true,
            AllowMultipleSelfHeals = true
        };
        await roleSettingsRepository.AddRoleSettings(DefaultRoleSettings);

        return newRoomId;
    }

    public bool DoesRoomExist(string roomId)
    {
        return roomRepository.DoesRoomExist(roomId);
    }

    public async Task AddPlayerToRoom(Guid playerId, AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var roomId = addEditPlayerDetails.RoomId;
        var isPlayerAlreadyInRoom = playerRoomRepository.IsPlayerInRoom(playerId, roomId);
        PlayerRoomEntity player;
        if (!isPlayerAlreadyInRoom)
        {
            if (addEditPlayerDetails.NickName == null)
            {
                throw new Exception("Player details are required for new player");
            }

            player = await playerRoomRepository.AddPlayerToRoom(playerId, addEditPlayerDetails);
        }
        else
        {
            player = playerRoomRepository.GetPlayerInRoomUsingPlayerGuid(roomId, playerId);
        }

        var currentMod = GetModeratorForRoom(roomId);
        if (currentMod == null)
        {
            await UpdateModeratorForRoom(roomId, player.Id);
        }
    }

    public PlayerDTO? GetModeratorForRoom(string roomId)
    {
        var room = roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }

        var mod = room.CurrentModeratorId;
        if (!mod.HasValue)
        {
            return null;
        }

        var moderatorDetails = playerRoomRepository.GetPlayerInRoom(roomId, mod.Value);
        return mapper.Map<PlayerDTO>(moderatorDetails);
    }

    public async Task<PlayerDTO> UpdateModeratorForRoom(string roomId, int newModeratorPlayerRoomId)
    {
        var room = roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }
        var newMod = playerRoomRepository.GetPlayerInRoom(roomId, newModeratorPlayerRoomId);
        room.CurrentModeratorId = newModeratorPlayerRoomId;
        await roomRepository.UpdateRoom(room);
        return mapper.Map<PlayerDTO>(newMod);
    }

    public async Task<PlayerDTO> UpdatePlayerDetailsForRoom(int playerRoomId,
        AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var roomId = addEditPlayerDetails.RoomId;
        var player = playerRoomRepository.GetPlayerInRoom(roomId, playerRoomId);
        if (player == null)
        {
            throw new PlayerNotFoundException($"Player with id {playerRoomId} does not exist");
        }

        if (addEditPlayerDetails.NickName == null)
        {
            throw new Exception("Nick name is required");
        }

        player.NickName = addEditPlayerDetails.NickName;
        player.AvatarIndex = addEditPlayerDetails.AvatarIndex.GetValueOrDefault(0);
        var updatedPlayer = await playerRoomRepository.UpdatePlayerInRoom(roomId, player.PlayerId, player);
        return mapper.Map<PlayerDTO>(updatedPlayer);
    }

    public async Task RemovePlayerFromRoom(string roomId, int playerRoomId)
    {
        await playerRoomRepository.RemovePlayerFromRoom(roomId, playerRoomId);
        //Replace Mod if player was mod
        var room = roomRepository.GetRoom(roomId);
        var otherPlayers = playerRoomRepository.GetPlayersInRoom(roomId);
        var newModerator = otherPlayers.FirstOrDefault()?.Id;
        if (room.CurrentModeratorId == null)
        {
            room.CurrentModeratorId = newModerator;
        }
        await roomRepository.UpdateRoom(room);
    }

    public int GetPlayerCountForRoom(string roomId)
    {
        return playerRoomRepository.GetPlayerCountForRoom(roomId);
    }

    public async Task UpdateRoomGameState(string roomId, GameState gameState)
    {
        var room = roomRepository.GetRoom(roomId);
        room.GameState = gameState;
        await roomRepository.UpdateRoom(room);
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