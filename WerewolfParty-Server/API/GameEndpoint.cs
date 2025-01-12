using FluentValidation;
using Microsoft.AspNetCore.SignalR;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;
using WerewolfParty_Server.Enum;
using WerewolfParty_Server.Extensions;
using WerewolfParty_Server.Hubs;
using WerewolfParty_Server.Models.Request;
using WerewolfParty_Server.Service;

namespace WerewolfParty_Server.API;

public static class GameEndpoint
{
    public static void RegisterGameEndpoints(this WebApplication app)
    {

        


        
        app.MapGet("/api/game/{roomId}/assigned-role", (HttpContext httpContext, GameService gameService, string roomId) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var assignedRole = gameService.GetAssignedPlayerRole(roomId, playerGuid);
            return TypedResults.Ok(new APIResponse<RoleName?>()
            {
                Success = true,
                Data = assignedRole
            });
        }).RequireAuthorization();
        
        app.MapGet("/api/game/{roomId}/all-player-roles", (HttpContext httpContext, GameService gameService, RoomService roomService, string roomId) =>
        {
            var playerGuid = httpContext.User.GetPlayerId();
            var currentModerator = roomService.GetModeratorForRoom(roomId);
            if (playerGuid != currentModerator.Id)
            {
                throw new Exception("You are not the moderator of this room.");
            }
            var assignedRoles = gameService.GetAllAssignedPlayerRolesAndActions(roomId);
            return TypedResults.Ok(new APIResponse<List<PlayerRoleActionDto>>()
            {
                Success = true,
                Data = assignedRoles
            });
        }).RequireAuthorization();
        
   
        
        app.MapGet("/api/game/{roomId}/{playerGuid:guid}/role-actions", (HttpContext httpContext, GameService gameService, string roomId, Guid playerGuid) =>
        {
            var state = gameService.GetActionsForPlayerRole(roomId,playerGuid);
            return TypedResults.Ok(new APIResponse<List<RoleActionDto>>()
            {
                Success = true,
                Data = state
            });
        }).RequireAuthorization();
    }
}