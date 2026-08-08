namespace PlayGround.Shared.Result;

/// <summary>
/// DetailCode에 대한 확장 메서드들
/// </summary>
public static class DetailCodeExtensions
{

    /// <summary>
    /// 에러 코드가 특정 범위에 속하는지 확인
    /// </summary>
    public static bool IsInRange(this DetailCode code, int minValue, int maxValue)
    {
        return code.Value >= minValue && code.Value <= maxValue;
    }

    /// <summary>
    /// 에러 코드가 사용자 오류인지 확인 (Client + Auth + Resource: 1000-1299)
    /// </summary>
    public static bool IsUserError(this DetailCode code)
    {
        return DetailCodeRange.IsUserError(code.Value);
    }

    /// <summary>
    /// 에러 코드가 시스템 오류인지 확인 (3000-3999)
    /// </summary>
    public static bool IsSystemError(this DetailCode code)
    {
        return DetailCodeRange.IsSystemError(code.Value);
    }

    /// <summary>
    /// 에러 코드가 비즈니스 로직 오류인지 확인 (Business + Sports: 2000-2199)
    /// </summary>
    public static bool IsBusinessError(this DetailCode code)
    {
        return DetailCodeRange.IsBusinessLogicError(code.Value);
    }

    /// <summary>
    /// HTTP 상태 코드로 변환
    /// </summary>
    public static int ToHttpStatusCode(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when code == ErrorCode.NotFound => 404,
            ErrorCode when code == ErrorCode.Unauthorized => 401,
            ErrorCode when code == ErrorCode.Forbidden => 403,
            ErrorCode when code == ErrorCode.BadRequest => 400,
            ErrorCode when code == ErrorCode.Conflict => 409,
            ErrorCode when code == ErrorCode.Gone => 410,
            ErrorCode when code == ErrorCode.TooManyRequests => 429,
            ErrorCode when code == ErrorCode.ServiceUnavailable => 503,
            ErrorCode when code == ErrorCode.MaintenanceMode => 503,
            ErrorCode when code.IsUserError() => 400,
            ErrorCode when code.IsBusinessError() => 422,
            ErrorCode when code.IsSystemError() => 500,
            ErrorCode => 500,
            WarningCode => 200,
            SuccessCode => 200,
            InformationCode => 200,
            _ => 200
        };
    }

    /// <summary>
    /// 재시도 가능한지 확인
    /// </summary>
    public static bool IsRetryable(this DetailCode code)
    {
        if (code is ErrorCode errorCode)
        {
            return errorCode.IsRetryable;
        }
        return false;
    }

}
