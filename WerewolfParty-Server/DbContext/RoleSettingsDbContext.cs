using Microsoft.EntityFrameworkCore;
using WerewolfParty_Server.Entities;

namespace WerewolfParty_Server.DbContext;

public class RoleSettingsDbContext(DbContextOptions<RoleSettingsDbContext> options)
    : Microsoft.EntityFrameworkCore.DbContext(options)
{
    public DbSet<RoleSettingsEntity> RoleSettings { get; set; }
}