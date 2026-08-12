namespace WerewolfParty_Server.DTO;

/// <summary>
/// The subject of an investigation. Deliberately does NOT carry the player's role — an
/// investigation only answers the yes/no question it was asked, otherwise the endpoint
/// hands out the whole game.
/// </summary>
public class InvestigatedPlayerDTO
{
    public int Id { get; set; }
    public string Nickname { get; set; }
}
