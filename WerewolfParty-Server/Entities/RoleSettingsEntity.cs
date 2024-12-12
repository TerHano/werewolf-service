using System.ComponentModel.DataAnnotations;
using WerewolfParty_Server.Enum;

namespace WerewolfParty_Server.Entities;

public class RoleSettingsEntity
{
    public int Id { get; set; }
    [MaxLength(10)] public required string RoomId { get; init; }
    public NumberOfWerewolves Werewolves { get; set; }
    public List<RoleName> SelectedRoles { get; set; } = new();
}