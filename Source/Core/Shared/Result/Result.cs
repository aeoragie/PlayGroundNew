// ErrorOr (https://github.com/amantinband/error-or)
// OneOf (https://github.com/mcintyre321/OneOf)

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PlayGround.Shared.Primitives;

namespace PlayGround.Shared.Result;

/// <summary>
/// 함수형 결과. 널 허용 여부는 타입 인자가 정한다 — <c>Result&lt;Team?&gt;</c>은 "성공인데 값 없음"을
/// 표현하는 별개의 계약이라 <see cref="Value"/>는 <c>T?</c>가 아니라 <c>T</c>다.
/// </summary>
public readonly struct Result<T>
{
    private readonly T mValue;

    public T Value
    {
        get
        {
            if (IsError)
            {
                Panic.Fail($"Value accessed on a failed result. {ResultData}");
            }

            return mValue;
        }
    }

    public ResultInfo ResultData { get; }
    public string Message => ResultData.Message;

    public bool IsSuccess => ResultData.IsSuccess;
    public bool IsError => ResultData.IsError;
    public bool IsWarning => ResultData.IsWarning;
    public bool IsInformation => ResultData.IsInformation;
    public bool IsFailure => !IsSuccess;

    private Result(T value)
    {
        mValue = value;
        ResultData = ResultInfo.Success();
    }

    private Result(T value, ResultInfo? info)
    {
        mValue = value;
        ResultData = info ?? ResultInfo.Success();
    }

    private Result(ResultInfo info)
    {
        mValue = default!;
        ResultData = info;
    }

    public static Result<T> Unknown() => new(ResultInfo.Unknown());

    public static Result<T> Success(T value) => new(value);

    public static Result<T> Failure(ResultInfo info)
    {
        if (info.IsSuccess)
        {
            Panic.Fail("Failure requires a non-success ResultInfo — the value would be lost.");
        }

        return new(info);
    }

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

            return Panic.Fail<Result<T>>($"FromDetailCode requires a value for non-error code '{detailCode}'.");
        }

        return detailCode.Category switch
        {
            ResultCodes.Success => Result<T>.Success(value),
            ResultCodes.Error when detailCode is ErrorCode errorCode => Result<T>.Error(errorCode),
            ResultCodes.Warning when detailCode is WarningCode warningCode => Result<T>.Warning(value, warningCode),
            ResultCodes.Information when detailCode is InformationCode infoCode => Result<T>.Information(value, infoCode),
            _ => Panic.Fail<Result<T>>($"DetailCode '{detailCode}' does not match its category '{detailCode.Category}'.")
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

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<ResultInfo, TResult> onError, Func<T, ResultInfo, TResult>? onWarning = null, Func<T, ResultInfo, TResult>? onInformation = null)
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
                return onError(ResultData);

            default:
                return Panic.Fail<TResult>($"Unknown result code: {ResultData.DetailCode.Category}");
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

    public T GetValueOrPanic()
    {
        if (!IsSuccess)
        {
            Panic.Fail(ResultData.ToString());
        }

        return Value!;
    }

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
            Panic.Fail("Failure requires a non-success ResultInfo.");
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
            _ => Panic.Fail<Result>($"DetailCode '{detailCode}' does not match its category '{detailCode.Category}'.")
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
