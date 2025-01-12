using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class RoomGameActionDbContext(DbContextOptions<RoomGameActionDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<RoomGameActionEntity> RoomGameActions { get; set; }
}