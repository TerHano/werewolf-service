using Microsoft.AspNetCore.SignalR;

namespace WerewolfParty_Server.Service;

public class NameUserIdProvider: IUserIdProvider
{
    
        public string GetUserId(HubConnectionContext connection)
        {
            return connection.User?.Identity?.Name;
        }
    
}