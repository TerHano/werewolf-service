namespace WerewolfParty_Server.Exceptions;

public class PlayerNotFoundException: Exception
{
    public PlayerNotFoundException(string message) : base(message) { }
}