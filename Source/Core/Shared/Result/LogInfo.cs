namespace PlayGround.Shared.Result;

public class LogInfo
{
    public string Level { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? OperationName { get; set; }
    public bool IsSuccess { get; set; }
    public int Priority { get; set; }
    public bool RequiresNotification { get; set; }
    public DateTime Timestamp { get; set; }
}
