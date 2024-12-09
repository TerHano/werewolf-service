using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class PlayerRoomDbContext(DbContextOptions<PlayerRoomDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<PlayerRoomEntity> PlayerRooms { get; set; }
}