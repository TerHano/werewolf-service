namespace WerewolfParty_Server.Models;

public class SocketResponse
{
    private string? ErrorMessage { get; set; }
    private bool Success { get; set; }

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