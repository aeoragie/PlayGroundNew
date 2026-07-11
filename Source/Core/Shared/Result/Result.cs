// ErrorOr (https://github.com/amantinband/error-or)
// OneOf (https://github.com/mcintyre321/OneOf)

namespace PlayGround.Shared.Result;

public readonly struct Result<T>
{
    public T? Value { get; }
    public ResultInfo ResultData { get; }
    public string Message => ResultData.Message;

    public bool IsSuccess => ResultData.IsSuccess;
    public bool IsError => ResultData.IsError;
    public bool IsWarning => ResultData.IsWarning;
    public bool IsInformation => ResultData.IsInformation;
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        Value = value;
        ResultData = ResultInfo.Success();
    }

    private Result(T value, ResultInfo? info)
    {
        Value = value;
        ResultData = info ?? ResultInfo.Success();
    }

    private Result(ResultInfo info)
    {
        Value = default;
        ResultData = info;
    }

    public static Result<T> Unknown() => new(ResultInfo.Unknown());

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(ResultInfo info) => new(info);

    public static Result<T> Error(ErrorCode code, string? message = null, string? details = null)
    {
        return new(ResultInfo.Error(code, message, details));
    }

    public static Result<T> Warning(T value, WarningCode code, string? message = null, string? details = null)
    {
        return new(value, ResultInfo.Warning(code, message, details));
    }

    public static Result<T> Information(T value, InformationCode code, string? message = null, string? details = null)
    {
        return new(value, ResultInfo.Information(code, message, details));
    }

    public static Result<T> FromDetailCode(DetailCode detailCode, T? value)
    {
        if (value is null)
        {
            if (detailCode is ErrorCode errorCode)
            {
                return Result<T>.Error(errorCode);
            }

            return Result<T>.Unknown();
        }

        return detailCode.Category switch
        {
            ResultCodes.Success => Result<T>.Success(value),
            ResultCodes.Error when detailCode is ErrorCode errorCode => Result<T>.Error(errorCode),
            ResultCodes.Warning when detailCode is WarningCode warningCode => Result<T>.Warning(value, warningCode),
            ResultCodes.Information when detailCode is InformationCode infoCode => Result<T>.Information(value, infoCode),
            _ => Result<T>.Unknown()
        };
    }

    public static Result<T> FromException(Exception ex, ErrorCode? errorCode = null)
    {
        var code = errorCode ?? MapExceptionToErrorCode(ex);
        return new(ResultInfo.Exception(ex, code));
    }

    private static ErrorCode MapExceptionToErrorCode(Exception ex)
    {
        return ex switch
        {
            ArgumentNullException => ErrorCode.MissingRequired,
            ArgumentException => ErrorCode.InvalidInput,
            UnauthorizedAccessException => ErrorCode.Unauthorized,
            TimeoutException => ErrorCode.NetworkTimeout,
            InvalidOperationException => ErrorCode.InvalidOperation,
            NotSupportedException => ErrorCode.OperationNotAllowed,
            _ => ErrorCode.UnknownError
        };
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<T, ResultInfo, TResult>? onWarning = null, Func<T, ResultInfo, TResult>? onInformation = null, Func<ResultInfo, TResult>? onError = null)
    {
        switch (ResultData.DetailCode.Category)
        {
            case ResultCodes.Success:
                return onSuccess(Value!);

            case ResultCodes.Warning:
                if (onWarning is not null)
                {
                    return onWarning.Invoke(Value!, ResultData);
                }
                return onSuccess(Value!);

            case ResultCodes.Information:
                if (onInformation is not null)
                {
                    return onInformation.Invoke(Value!, ResultData);
                }
                return onSuccess(Value!);

            case ResultCodes.Error:
                if (onError != null)
                {
                    return onError(ResultData);
                }
                throw new InvalidOperationException("Error handler not provided");

            default:
                throw new InvalidOperationException($"Unknown result code: {ResultData.DetailCode.Category}");
        }
    }

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultInfo, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value!) : onFailure(ResultData);
    }

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        return IsSuccess ? Result<TNew>.Success(mapper(Value!)) : Result<TNew>.Failure(ResultData);
    }

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> binder)
    {
        return IsSuccess ? binder(Value!) : Result<TNew>.Failure(ResultData);
    }

    public Result<T> OnSuccess(Action<T> action)
    {
        if (IsSuccess)
        {
            action(Value!);
        }
        return this;
    }

    public Result<T> OnError(Action<ResultInfo> action)
    {
        if (IsError)
        {
            action(ResultData);
        }
        return this;
    }

    public Result<T> OnWarning(Action<T, ResultInfo> action)
    {
        if (IsWarning)
        {
            action(Value!, ResultData);
        }
        return this;
    }

    public Result<T> OnInfo(Action<T, ResultInfo> action)
    {
        if (IsInformation)
        {
            action(Value!, ResultData);
        }
        return this;
    }

    public Result<T> OnErrorCode(ErrorCode errorCode, Action<ResultInfo> action)
    {
        if (IsError && ResultData.DetailCode == errorCode)
        {
            action(ResultData);
        }
        return this;
    }

    // ErrorCode 카테고리별 처리
    public Result<T> OnClientError(Action<ResultInfo> action)
    {
        if (IsError && ResultData.DetailCode is ErrorCode errorCode && errorCode.IsClientError)
        {
            action(ResultData);
        }
        return this;
    }

    public Result<T> OnSystemError(Action<ResultInfo> action)
    {
        if (IsError && ResultData.DetailCode is ErrorCode errorCode && errorCode.IsSystemError)
        {
            action(ResultData);
        }
        return this;
    }

    public T GetValueOrDefault(T defaultValue = default!) => IsSuccess ? Value! : defaultValue;

    public T GetValueOrThrow() => IsSuccess ? Value! : throw new InvalidOperationException(ResultData.ToString());

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(ResultInfo resultInfo) => Failure(resultInfo);

    public override string ToString() => IsSuccess ? $"Success({Value}) - {ResultData}" : $"Failure - {ResultData}";
}

//.//

public readonly struct Result
{
    public ResultInfo ResultData { get; }
    public string Message => ResultData.Message;

    public bool IsSuccess => ResultData.IsSuccess;
    public bool IsError => ResultData.IsError;
    public bool IsWarning => ResultData.IsWarning;
    public bool IsInformation => ResultData.IsInformation;
    public bool IsFailure => !IsSuccess;

    private Result(ResultInfo info)
    {
        ResultData = info;
    }

    public static Result Unknown() => new(ResultInfo.Unknown());

    public static Result Success() => new(ResultInfo.Success());

    public static Result Error(ErrorCode code, string? message = null, string? details = null)
    {
        return new(ResultInfo.Error(code, message, details));
    }

    public static Result Warning(WarningCode code, string? message = null, string? details = null)
    {
        return new(ResultInfo.Warning(code, message, details));
    }

    public static Result Information(InformationCode code, string? message = null, string? details = null)
    {
        return new(ResultInfo.Information(code, message, details));
    }

    public static Result Failure(ResultInfo info)
    {
        if (info.IsSuccess)
        {
            throw new ArgumentException("Cannot create a failure result from a success ResultInfo.", nameof(info));
        }

        return new(info);
    }

    public static Result FromDetailCode(DetailCode detailCode)
    {
        return detailCode.Category switch
        {
            ResultCodes.Success => Result.Success(),
            ResultCodes.Error when detailCode is ErrorCode errorCode => Result.Error(errorCode),
            ResultCodes.Warning when detailCode is WarningCode warningCode => Result.Warning(warningCode),
            ResultCodes.Information when detailCode is InformationCode infoCode => Result.Information(infoCode),
            _ => Result.Unknown()
        };
    }

    public static Result FromException(Exception ex, ErrorCode? errorCode = null)
    {
        if (errorCode is null)
        {
            return new(ResultInfo.Exception(ex, ErrorCode.UnknownError));
        }
        else
        {
            return new(ResultInfo.Exception(ex, errorCode));
        }
    }

    public TResult Match<TResult>(Func<TResult> onSuccess, Func<ResultInfo, TResult> onFailure)
    {
        if (IsSuccess)
        {
            return onSuccess();
        }
        else
        {
            return onFailure(ResultData);
        }
    }

    public Result OnSuccess(Action onAction)
    {
        if (IsSuccess)
        {
            onAction();
        }
        return this;
    }

    public Result OnError(Action<ResultInfo> onAction)
    {
        if (IsError)
        {
            onAction(ResultData);
        }
        return this;
    }

    public Result OnWarning(Action<ResultInfo> onAction)
    {
        if (IsWarning)
        {
            onAction(ResultData);
        }
        return this;
    }

    public Result OnInfo(Action<ResultInfo> onAction)
    {
        if (IsInformation)
        {
            onAction(ResultData);
        }
        return this;
    }

    public static implicit operator Result(ResultInfo resultInfo) => new(resultInfo);

    public override string ToString() => IsSuccess ? "Success" : $"Failure - {ResultData}";
}
