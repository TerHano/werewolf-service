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
        modelBuilder.Entity<RoomSettingsEntity>()
            .HasOne(e => e.Room)
            .WithOne(e => e.RoleSettings)
            .HasForeignKey<RoomSettingsEntity>(e => e.RoomId);

        modelBuilder.Entity<RoomSettingsEntity>()
            .HasOne(e => e.Room)
            .WithOne(e => e.RoleSettings)
            .HasForeignKey<RoomSettingsEntity>(e => e.RoomId);

        // Must agree with the entity initialiser, so a row inserted without the property set
        // does not silently disagree with what the code thinks it wrote.
        modelBuilder.Entity<RoomSettingsEntity>()
            .Property(e => e.SelfModerated)
            .HasDefaultValue(true);

        // Without an explicit default this lands as 0, and a zero-second step expires the
        // instant it starts — the night would race through every step in one clock tick.
        modelBuilder.Entity<RoomSettingsEntity>()
            .Property(e => e.NightStepSeconds)
            .HasDefaultValue(45);

        modelBuilder.Entity<RoomGameActionEntity>()
            .HasOne(e => e.PlayerRole)
            .WithMany()
            .HasForeignKey(e => e.PlayerRoleId);

        modelBuilder.Entity<RoomGameActionEntity>()
            .HasOne(e => e.AffectedPlayerRole)
            .WithMany()
            .HasForeignKey(e => e.AffectedPlayerRoleId);

        // One row per device. Unique so a re-subscribing phone updates its row instead of
        // collecting duplicates that each deliver the same notification.
        modelBuilder.Entity<PushSubscriptionEntity>()
            .HasIndex(e => e.Endpoint)
            .IsUnique();

        modelBuilder.Entity<PushSubscriptionEntity>()
            .HasIndex(e => e.PlayerId);
    }

    public DbSet<RoomGameActionEntity> RoomGameActions { get; set; }
    public DbSet<RoomEntity> Rooms { get; set; }
    public DbSet<PlayerRoleEntity> PlayerRoles { get; set; }
    public DbSet<PlayerRoomEntity> PlayerRooms { get; set; }
    public DbSet<RoomSettingsEntity> RoleSettings { get; set; }
    public DbSet<PushSubscriptionEntity> PushSubscriptions { get; set; }
}