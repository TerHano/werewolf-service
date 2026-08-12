namespace WerewolfParty_Server.Models;

public class SocketResponse
{
    // Must be public: System.Text.Json only serialises public members, so private ones made
    // every hub response an empty object and the client could never read success or the error.
    public string? ErrorMessage { get; set; }
    public bool Success { get; set; }

    public SocketResponse(bool success)
    {
        this.Success = success;
    }

    public SocketResponse(bool success, string errorMessage)
    {
        this.Success = success;
        this.ErrorMessage = errorMessage;
    }
}