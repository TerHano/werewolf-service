using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class WerewolfDbContext(DbContextOptions<WerewolfDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerRoleEntity>()
            .HasOne(e => e.PlayerRoom)
            .WithOne(e => e.PlayerRole)
            .HasForeignKey<PlayerRoleEntity>(e => e.PlayerRoomId);
        modelBuilder.Entity<PlayerRoomEntity>()
            .HasOne(e => e.Room)
            .WithMany(e => e.PlayersInRoom)
            .HasForeignKey(e => e.RoomId);
        modelBuilder.Entity<RoleSettingsEntity>()
            .HasOne(e => e.Room)
            .WithOne(e => e.RoleSettings)
            .HasForeignKey<RoleSettingsEntity>(e => e.RoomId);
        
        modelBuilder.Entity<RoleSettingsEntity>()
            .HasOne(e => e.Room)
            .WithOne(e => e.RoleSettings)
            .HasForeignKey<RoleSettingsEntity>(e => e.RoomId);
       

        
        
    }
    public DbSet<RoomGameActionEntity> RoomGameActions { get; set; }
    public DbSet<RoomEntity> Rooms { get; set; }
    public DbSet<PlayerRoleEntity> PlayerRoles { get; set; }
    public DbSet<PlayerRoomEntity> PlayerRooms { get; set; }
    public DbSet<RoleSettingsEntity> RoleSettings { get; set; }
}