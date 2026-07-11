namespace PlayGround.Shared.Result;

public class HttpResponseInfo
{
    public int StatusCode { get; set; }
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public object? Value { get; set; }
    public string? Details { get; set; }
    public DateTime Timestamp { get; set; }
}
