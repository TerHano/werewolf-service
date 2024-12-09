using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class RoomDbContext(DbContextOptions<RoomDbContext> options) : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<RoomEntity> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RoomEntity>()
            .HasMany(r => r.PlayersInRooms)
            .WithOne(r => r.Room)
            .HasForeignKey(e => e.RoomId)
            .HasPrincipalKey(e => e.Id);
    }
}