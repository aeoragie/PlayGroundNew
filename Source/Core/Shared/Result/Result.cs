// ErrorOr (https://github.com/amantinband/error-or)
// OneOf (https://github.com/mcintyre321/OneOf)

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using PlayGround.Shared.Primitives;

namespace PlayGround.Shared.Result;

/// <summary>
/// 함수형 결과.
///
/// **널 허용 여부는 타입 인자가 정한다.** <c>Result&lt;Team&gt;</c>은 성공하면 값이 있다는 뜻이고,
/// <c>Result&lt;Team?&gt;</c>·<c>Result&lt;Guid?&gt;</c>는 **성공인데 값이 없음**(= 조회 결과 없음·권한 없음)을
/// 표현하는 별개의 계약이다. 그래서 <see cref="Value"/>는 <c>T?</c>가 아니라 <c>T</c>다 —
/// <c>T?</c>로 두면 두 계약이 뭉개져 `Result&lt;Team&gt;`을 쓰는 호출부마다 의미 없는 널 검사가 붙는다.
///
/// 실패 경로(Error·Unknown·Failure·FromException)는 값을 담지 않으므로,
/// <see cref="Value"/>는 **<see cref="IsError"/>를 확인한 뒤에** 읽어야 한다(기존 사용법 그대로).
/// </summary>
public readonly struct Result<T>
{
    /// <summary>결과 값. 실패 경로에서는 채워지지 않으니 <see cref="IsError"/> 확인 후 읽는다.</summary>
    public T Value { get; }

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
        // 실패 경로 — 값이 없다. T가 널 비허용이어도 여기서는 담을 값이 없으므로 억제한다
        Value = default!;
        ResultData = info;
    }

    public static Result<T> Unknown() => new(ResultInfo.Unknown());

    public static Result<T> Success(T value) => new(value);

    /// <summary>다른 Result의 실패를 이 타입으로 옮긴다. 값을 담지 않으므로 **오류 정보여야 한다** —
    /// 실패가 아닌 정보를 넘기면 "성공인데 값 없음"이 되어 호출부가 값을 읽다 터진다.</summary>
    public static Result<T> Failure(ResultInfo info)
    {
        Debug.Assert(info.IsError, "Failure expects an error ResultInfo");
        return new(info.IsError ? info : ResultInfo.Unknown(info.Message));
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
                Debug.Assert(false, $"Unknown result code: {ResultData.DetailCode.Category}");
                return onError(ResultData);
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
        Debug.Assert(!info.IsSuccess, "Failure expects a non-success ResultInfo");
        return new(info.IsSuccess ? ResultInfo.Unknown(info.Message) : info);
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
