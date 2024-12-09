namespace WerewolfParty_Server.DTO;

public class APIResponse<T>
{
    public bool Success { get; set; }
    public  IEnumerable<string> ErrorMessages { get; set; }
    public T Data { get; set; }
}

public class APIResponse
{
    public bool Success { get; set; }
    public IEnumerable<string> ErrorMessages { get; set; }}