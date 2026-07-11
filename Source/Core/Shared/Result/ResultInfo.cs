namespace PlayGround.Shared.Result;

public readonly struct ResultInfo
{
    public DetailCode DetailCode { get; }
    public string Message { get; }
    public string? Details { get; }

    private ResultInfo(DetailCode detailCode, string? message, string? details)
    {
        DetailCode = detailCode;
        Message = message ?? detailCode.DefaultMessage;
        Details = details;
    }

    // Success
    public static ResultInfo Success(string? details = null)
    {
        return new(SuccessCode.Ok, null, details);
    }

    // Error
    public static ResultInfo Unknown(string? details = null)
    {
        return new(ErrorCode.UnknownError, null, details);
    }

    public static ResultInfo Error(ErrorCode code, string? message = null, string? details = null)
    {
        return new(code, message, details);
    }

    // Warning
    public static ResultInfo Warning(WarningCode code, string? message = null, string? details = null)
    {
        return new(code, message, details);
    }

    // Information
    public static ResultInfo Information(InformationCode code, string? message = null, string? details = null)
    {
        return new(code, message, details);
    }

    // Exception
    public static ResultInfo Exception(Exception ex, ErrorCode? code = null)
    {
        var errorCode = code ?? ErrorCode.UnknownError;
        return new(errorCode, ex.Message, ex.StackTrace);
    }

    public bool IsSuccess => DetailCode.Category == ResultCodes.Success;
    public bool IsError => DetailCode.Category == ResultCodes.Error;
    public bool IsWarning => DetailCode.Category == ResultCodes.Warning;
    public bool IsInformation => DetailCode.Category == ResultCodes.Information;

    public override string ToString()
    {
        var result = $"[{DetailCode.Category}:{DetailCode.Name}] {Message}";
        return string.IsNullOrEmpty(Details) ? result : $"{result} - {Details}";
    }
}
