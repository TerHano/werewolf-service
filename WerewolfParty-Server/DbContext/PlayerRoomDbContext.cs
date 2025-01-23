using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class PlayerRoomDbContext(DbContextOptions<PlayerRoomDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    
    // protected override void OnModelCreating(ModelBuilder modelBuilder)
    // {
    //     modelBuilder.Entity<PlayerRoomEntity>()
    //         .HasOne(e => e.PlayerRole)
    //         .WithOne(e => e.PlayerRoom)
    //         .HasForeignKey<PlayerRoleEntity>(e=>e.PlayerId)
    //         .HasPrincipalKey<PlayerRoomEntity>(e => e.PlayerId);
    // }
    public DbSet<PlayerRoomEntity> PlayerRooms { get; set; }
}