namespace PlayGround.Shared.Result;

/// <summary>
/// DetailCode에 대한 확장 메서드들
/// </summary>
public static class DetailCodeExtensions
{
    public static string GetMessage(this DetailCode errorCode)
    {
        return errorCode.DefaultMessage ?? "Unknown status";
    }

    public static T? As<T>(this DetailCode detailCode) where T : DetailCode
    {
        return detailCode as T;
    }

    public static bool TryAs<T>(this DetailCode detailCode, out T? result) where T : DetailCode
    {
        result = detailCode as T;
        return result != null;
    }

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
    /// 로그 레벨 결정
    /// </summary>
    public static string GetLogLevel(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when ((ErrorCode)code).IsCritical => "Fatal",
            ErrorCode when code.IsSystemError() => "Error",
            ErrorCode when code.IsBusinessError() => "Warning",
            ErrorCode when code.IsUserError() => "Information",
            WarningCode => "Warning",
            InformationCode => "Information",
            SuccessCode => "Information",
            _ => "Information"
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

    /// <summary>
    /// 사용자에게 표시 가능한지 확인
    /// </summary>
    public static bool IsUserFriendly(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when code.IsUserError() => true,
            ErrorCode when code.IsBusinessError() => true,
            WarningCode => true,
            InformationCode => true,
            SuccessCode => true,
            _ => false
        };
    }

    /// <summary>
    /// 메트릭 카테고리 가져오기
    /// </summary>
    public static string GetMetricCategory(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when code.IsUserError() => "client_error",
            ErrorCode when code.IsBusinessError() => "business_error",
            ErrorCode when code.IsSystemError() => "system_error",
            WarningCode => "warning",
            InformationCode => "information",
            SuccessCode => "success",
            _ => "unknown"
        };
    }

    /// <summary>
    /// 알림 필요 여부 확인
    /// </summary>
    public static bool RequiresNotification(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when ((ErrorCode)code).IsCritical => true,
            ErrorCode when code == ErrorCode.DatabaseError => true,
            ErrorCode when code == ErrorCode.ExternalServiceUnavailable => true,
            WarningCode when code.IsInRange(5600, 5699) => true, // System warnings
            _ => false
        };
    }

    /// <summary>
    /// 코드 우선순위 가져오기 (높을수록 중요)
    /// </summary>
    public static int GetPriority(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when ((ErrorCode)code).IsCritical => 5,
            ErrorCode when code.IsSystemError() => 4,
            ErrorCode when code.IsBusinessError() => 3,
            ErrorCode when code.IsUserError() => 2,
            WarningCode => 2,
            InformationCode => 1,
            SuccessCode => 1,
            _ => 0
        };
    }

    /// <summary>
    /// 사용자 친화적 메시지 생성
    /// </summary>
    public static string GetUserFriendlyMessage(this DetailCode code, string? customMessage = null)
    {
        if (!string.IsNullOrEmpty(customMessage) && code.IsUserFriendly())
        {
            return customMessage;
        }

        return code switch
        {
            ErrorCode when code.IsSystemError() => "We're sorry, but there's a temporary system issue. Please try again later.",
            ErrorCode when code == ErrorCode.NetworkTimeout => "Network connection is unstable. Please try again.",
            ErrorCode when code == ErrorCode.ServiceUnavailable => "Service is temporarily unavailable. Please try again later.",
            ErrorCode when code.IsUserError() => code.DefaultMessage,
            ErrorCode when code.IsBusinessError() => code.DefaultMessage,
            _ => code.DefaultMessage
        };
    }

    /// <summary>
    /// 오류 해결 방법 제안
    /// </summary>
    public static string? GetResolutionSuggestion(this DetailCode code)
    {
        return code switch
        {
            ErrorCode when code == ErrorCode.InvalidInput => "Please check your input and enter it in the correct format.",
            ErrorCode when code == ErrorCode.MissingRequired => "Please fill in all required fields.",
            ErrorCode when code == ErrorCode.Unauthorized => "Please log in and try again.",
            ErrorCode when code == ErrorCode.Forbidden => "You don't have permission for this operation. Please contact the administrator.",
            ErrorCode when code == ErrorCode.NotFound => "The requested information could not be found. Please check the URL.",
            ErrorCode when code == ErrorCode.TooManyRequests => "Too many requests occurred. Please try again later.",
            ErrorCode when code == ErrorCode.NetworkTimeout => "Please check your network connection and try again.",
            ErrorCode when code == ErrorCode.ServiceUnavailable => "Please wait a moment for service recovery.",
            _ => null
        };
    }
}
