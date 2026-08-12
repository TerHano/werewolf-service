using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WerewolfParty_Server.Entities;

/// <summary>
/// One push endpoint for one device.
///
/// Keyed on the player GUID from the JWT rather than on a room, so a subscription survives
/// leaving one room and joining the next — a phone subscribes once and keeps working all
/// evening. A player with two devices simply has two rows and both buzz.
/// </summary>
[Table("push_subscription")]
public class PushSubscriptionEntity
{
    [Key] [Column("id")] public int Id { get; set; }

    [Column("player_id")] public required Guid PlayerId { get; set; }

    /// <summary>
    /// The browser's push service URL. Unique: re-subscribing the same device must update the
    /// existing row rather than pile up duplicates that all deliver the same notification.
    /// </summary>
    [Column("endpoint")] public required string Endpoint { get; set; }

    [Column("p256dh")] public required string P256dh { get; set; }
    [Column("auth")] public required string Auth { get; set; }

    [Column("created_date")] public required DateTime CreatedDate { get; set; }
    [Column("last_seen_date")] public required DateTime LastSeenDate { get; set; }
}
