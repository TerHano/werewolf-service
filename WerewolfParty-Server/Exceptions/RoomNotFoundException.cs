namespace WerewolfParty_Server.Exceptions;

public class RoomNotFoundException : Exception
{
    public RoomNotFoundException(string message) : base(message)
    {
    }
}