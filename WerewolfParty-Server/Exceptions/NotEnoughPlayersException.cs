namespace WerewolfParty_Server.Exceptions;

public class NotEnoughPlayersException : Exception
{
    public NotEnoughPlayersException(string message) : base(message)
    {
    }
}