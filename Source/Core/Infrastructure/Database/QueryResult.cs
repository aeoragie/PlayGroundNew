namespace PlayGround.Infrastructure.Database;

public enum QueryResult
{
    None = 0,
    Success,
    Error,
    Exception,
    NotFound,
    Duplicate,
    Timeout,
}
