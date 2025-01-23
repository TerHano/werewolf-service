using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.DTO;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class PlayerRoleDbContext(DbContextOptions<PlayerRoleDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PlayerRoleEntity>()
            .HasOne(e => e.PlayerRoom)
            .WithOne(e => e.PlayerRole)
            .HasForeignKey<PlayerRoleEntity>(e => e.PlayerRoomId);
       
    }
    public DbSet<PlayerRoleEntity> PlayerRoles { get; set; }
}