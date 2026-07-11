namespace PlayGround.Shared.Result;

public class MetricInfo
{
    public string OperationName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsRetryable { get; set; }
    public int Priority { get; set; }
    public TimeSpan Duration { get; set; }
    public DateTime Timestamp { get; set; }
}
