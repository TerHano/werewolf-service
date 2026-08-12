namespace WerewolfParty_Server.DTO;

/// <summary>
/// A player in the game as everyone at the table sees them: a name, a face, and whether they
/// are still alive. Keyed by <c>player_role</c> id so it lines up with the target ids in
/// <see cref="RoleActionDto.ValidPlayerIds"/>.
///
/// Deliberately carries **no role**. Who is playing and who has died is public — it is said out
/// loud every morning — but what they are is the game.
/// </summary>
public class GamePlayerDto
{
    public int Id { get; set; }
    public string Nickname { get; set; } = string.Empty;
    public int AvatarIndex { get; set; }
    public bool IsAlive { get; set; }
}
