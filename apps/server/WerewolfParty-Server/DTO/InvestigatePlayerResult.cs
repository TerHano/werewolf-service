namespace WerewolfParty_Server.DTO;

public class InvestigatePlayerResult
{
    public InvestigatedPlayerDTO PlayerRole { get; set; }
    public bool IsInvestigationSuccessful { get; set; } 
}