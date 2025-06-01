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

    public async Task<List<RoomEntity>> GetAllRooms()
    {
        return await roomRepository.GetAllRooms();
    }

    public async Task<RoomEntity> GetRoom(string roomId)
    {
        return await roomRepository.GetRoom(roomId);
    }

    public async Task<PlayerDTO> GetPlayerInRoomUsingGuid(string roomId, Guid playerId)
    {
        var player = await playerRoomRepository.GetPlayerInRoomUsingPlayerGuid(roomId, playerId);
        return mapper.Map<PlayerDTO>(player);
    }

    public async Task<PlayerDTO> GetPlayerInRoom(string roomId, int playerId)
    {
        var player = await playerRoomRepository.GetPlayerInRoom(roomId, playerId);
        return mapper.Map<PlayerDTO>(player);
    }

    public async Task<bool> isPlayerInRoom(string roomId, Guid playerId)
    {
        return await playerRoomRepository.IsPlayerInRoom(playerId, roomId);
    }

    public async Task<RoomSettingsDto> GetRoleSettingsForRoom(string roomId)
    {
        var settings = await roleSettingsRepository.GetRoomSettingsByRoomId(roomId);
        return mapper.Map<RoomSettingsDto>(settings);
    }

    public async Task UpdateRoleSettingsForRoom(UpdateRoleSettingsRequest updateRoleSettingsRequest)
    {
        var oldRoleSettings = await roleSettingsRepository.GetRoomSettingsById(updateRoleSettingsRequest.Id);
        oldRoleSettings.NumberOfWerewolves = updateRoleSettingsRequest.NumberOfWerewolves;
        oldRoleSettings.SelectedRoles = updateRoleSettingsRequest.SelectedRoles;
        oldRoleSettings.ShowGameSummary = updateRoleSettingsRequest.ShowGameSummary;
        oldRoleSettings.AllowMultipleSelfHeals = updateRoleSettingsRequest.AllowMultipleSelfHeals;
        await roleSettingsRepository.UpdateRoleSettings(oldRoleSettings);
    }


    public async Task<List<PlayerDTO>> GetAllPlayersInRoom(string roomId, Guid? playerId = null, bool includeModerator = true)
    {
        var room = await roomRepository.GetRoom(roomId);
        var players = includeModerator
            ? await playerRoomRepository.GetPlayersInRoom(roomId)
            : await playerRoomRepository.GetPlayersInRoomWithoutModerator(roomId, room.CurrentModeratorId);
        if (!playerId.HasValue) return mapper.Map<List<PlayerDTO>>(players);
        var currentPlayer = players.FirstOrDefault((player) => player.PlayerId.Equals(playerId.Value));
        if (currentPlayer == null) return mapper.Map<List<PlayerDTO>>(players);
        players.Remove(currentPlayer);
        players.Insert(0, currentPlayer);
        return mapper.Map<List<PlayerDTO>>(players);
    }

    public async Task<string> CreateRoom()
    {
        var newRoomId = await GenerateRoomId();
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
        var defaultRoleSettings = new RoomSettingsEntity()
        {
            RoomId = newRoomId,
            NumberOfWerewolves = 1,
            SelectedRoles = [RoleName.Doctor, RoleName.Detective, RoleName.Witch],
            ShowGameSummary = true,
            AllowMultipleSelfHeals = true
        };
        await roleSettingsRepository.AddRoleSettings(defaultRoleSettings);

        return newRoomId;
    }

    public async Task<bool> DoesRoomExist(string roomId)
    {
        return await roomRepository.DoesRoomExist(roomId);
    }

    public async Task AddPlayerToRoom(Guid playerId, AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var roomId = addEditPlayerDetails.RoomId;
        var isPlayerAlreadyInRoom = await playerRoomRepository.IsPlayerInRoom(playerId, roomId);
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
            player = await playerRoomRepository.GetPlayerInRoomUsingPlayerGuid(roomId, playerId);
        }

        var currentMod = await GetModeratorForRoom(roomId);
        if (currentMod == null)
        {
            await UpdateModeratorForRoom(roomId, player.Id);
        }
    }

    public async Task<PlayerDTO?> GetModeratorForRoom(string roomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }

        var mod = room.CurrentModeratorId;
        if (!mod.HasValue)
        {
            return null;
        }

        var moderatorDetails = await playerRoomRepository.GetPlayerInRoom(roomId, mod.Value);
        return mapper.Map<PlayerDTO>(moderatorDetails);
    }

    public async Task<PlayerDTO> UpdateModeratorForRoom(string roomId, int newModeratorPlayerRoomId)
    {
        var room = await roomRepository.GetRoom(roomId);
        if (room == null)
        {
            throw new Exception($"Room with id {roomId} does not exist");
        }
        var newMod =await playerRoomRepository.GetPlayerInRoom(roomId, newModeratorPlayerRoomId);
        room.CurrentModeratorId = newModeratorPlayerRoomId;
        await roomRepository.UpdateRoom(room);
        return mapper.Map<PlayerDTO>(newMod);
    }

    public async Task<PlayerDTO> UpdatePlayerDetailsForRoom(int playerRoomId,
        AddEditPlayerDetailsDTO addEditPlayerDetails)
    {
        var roomId = addEditPlayerDetails.RoomId;
        var player = await playerRoomRepository.GetPlayerInRoom(roomId, playerRoomId);
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
        var room = await roomRepository.GetRoom(roomId);
        var otherPlayers = await playerRoomRepository.GetPlayersInRoom(roomId);
        var newModerator = otherPlayers.FirstOrDefault()?.Id;
        if (room.CurrentModeratorId == null)
        {
            room.CurrentModeratorId = newModerator;
        }
        await roomRepository.UpdateRoom(room);
    }

    public async Task<int?> GetPlayerCountForRoom(string roomId)
    {
        return await playerRoomRepository.GetPlayerCountForRoom(roomId);
    }

    public async Task UpdateRoomGameState(string roomId, GameState gameState)
    {
        var room = await roomRepository.GetRoom(roomId);
        room.GameState = gameState;
        await roomRepository.UpdateRoom(room);
    }

    private async Task<string> GenerateRoomId()
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
            isUniqueRoomId = await DoesRoomExist(generatedRoomId) == false;
        }

        return generatedRoomId;
    }
}