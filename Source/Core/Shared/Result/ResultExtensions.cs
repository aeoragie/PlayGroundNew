using PlayGround.Shared.Http;

namespace PlayGround.Shared.Result;

public static class ResultExtensions
{
    public static HttpResponseInfo ToHttpResponse<T>(this Result<T> result)
    {
        return new HttpResponseInfo
        {
            StatusCode = result.ResultData.DetailCode.ToHttpStatusCode(),
            IsSuccess = result.IsSuccess,
            Message = result.ResultData.DetailCode.GetUserFriendlyMessage(result.Message),
            Code = result.ResultData.DetailCode.Name,
            Value = result.Value,
            Details = result.ResultData.Details,
            Timestamp = DateTime.UtcNow
        };
    }

    public static HttpResponseInfo ToHttpResponse(this Result result)
    {
        return new HttpResponseInfo
        {
            StatusCode = result.ResultData.DetailCode.ToHttpStatusCode(),
            IsSuccess = result.IsSuccess,
            Message = result.ResultData.DetailCode.GetUserFriendlyMessage(result.Message),
            Code = result.ResultData.DetailCode.Name,
            Value = null,
            Details = result.ResultData.Details,
            Timestamp = DateTime.UtcNow
        };
    }

    public static LogInfo ToLogInfo<T>(this Result<T> result, string? operationName = null)
    {
        return CreateLogInfo(result.ResultData, result.IsSuccess, result.Message, operationName);
    }

    public static LogInfo ToLogInfo(this Result result, string? operationName = null)
    {
        return CreateLogInfo(result.ResultData, result.IsSuccess, result.Message, operationName);
    }

    private static LogInfo CreateLogInfo(ResultInfo resultData, bool isSuccess, string message, string? operationName)
    {
        return new LogInfo
        {
            Level = resultData.DetailCode.GetLogLevel(),
            Category = resultData.DetailCode.GetMetricCategory(),
            Code = resultData.DetailCode.Name,
            Message = message,
            Details = resultData.Details,
            OperationName = operationName,
            IsSuccess = isSuccess,
            Priority = resultData.DetailCode.GetPriority(),
            RequiresNotification = resultData.DetailCode.RequiresNotification(),
            Timestamp = DateTime.UtcNow
        };
    }

    public static MetricInfo ToMetricInfo<T>(this Result<T> result, string operationName, TimeSpan? duration = null)
    {
        return new MetricInfo
        {
            OperationName = operationName,
            Category = result.ResultData.DetailCode.GetMetricCategory(),
            Code = result.ResultData.DetailCode.Name,
            IsSuccess = result.IsSuccess,
            IsRetryable = result.ResultData.DetailCode.IsRetryable(),
            Priority = result.ResultData.DetailCode.GetPriority(),
            Duration = duration ?? TimeSpan.Zero,
            Timestamp = DateTime.UtcNow
        };
    }

    public static async Task<Result<T>> OnSuccessAsync<T>(this Result<T> result, Func<T, Task> action)
    {
        if (result.IsSuccess)
        {
            await action(result.Value!);
        }
        return result;
    }

    public static async Task<Result<T>> OnErrorAsync<T>(this Result<T> result, Func<ResultInfo, Task> action)
    {
        if (result.IsError)
        {
            await action(result.ResultData);
        }
        return result;
    }

    public static async Task<Result<T>> OnErrorCodeAsync<T>(this Result<T> result, ErrorCode errorCode, Func<ResultInfo, Task> action)
    {
        if (result.IsError && result.ResultData.DetailCode == errorCode)
        {
            await action(result.ResultData);
        }
        return result;
    }

    public static bool IsRetryable<T>(this Result<T> result)
    {
        return result.IsError && result.ResultData.DetailCode.IsRetryable();
    }

    public static bool IsUserFriendly<T>(this Result<T> result)
    {
        return result.ResultData.DetailCode.IsUserFriendly();
    }

    public static bool RequiresNotification<T>(this Result<T> result)
    {
        return result.ResultData.DetailCode.RequiresNotification();
    }

    public static Result<TNew> MapWhenValue<T, TNew>(this Result<T> result, Func<T, TNew> mapper, TNew defaultValue = default!)
    {
        if (result.Value == null)
        {
            return Result<TNew>.Failure(result.ResultData);
        }

        try
        {
            var newValue = mapper(result.Value);
            if (result.IsSuccess)
            {
                return Result<TNew>.Success(newValue);
            }

            if (result.IsWarning)
            {
                return Result<TNew>.Warning(newValue, (WarningCode)result.ResultData.DetailCode, result.Message, result.ResultData.Details);
            }

            if (result.IsInformation)
            {
                return Result<TNew>.Information(newValue, (InformationCode)result.ResultData.DetailCode, result.Message, result.ResultData.Details);
            }

            return Result<TNew>.Failure(result.ResultData);
        }
        catch (Exception ex)
        {
            return Result<TNew>.FromException(ex);
        }
    }

    public static Result<T[]> CombineAll<T>(params Result<T>[] results)
    {
        if (results.Length == 0)
        {
            return Result<T[]>.Success([]);
        }

        var errors = results.Where(r => r.IsError).ToList();
        if (errors.Count > 0)
        {
            return Result<T[]>.Failure(errors[0].ResultData);
        }

        var warnings = results.Where(r => r.IsWarning).ToList();
        if (warnings.Count > 0)
        {
            var values = results.Select(r => r.Value!).ToArray();
            var firstWarning = warnings[0];
            return Result<T[]>.Warning(values, (WarningCode)firstWarning.ResultData.DetailCode,
                $"Combined with {warnings.Count} warnings", firstWarning.ResultData.Details);
        }

        var successValues = results.Select(r => r.Value!).ToArray();
        return Result<T[]>.Success(successValues);
    }

    public static Result<T> CombineAny<T>(params Result<T>[] results)
    {
        if (results.Length == 0)
        {
            return Result<T>.Unknown();
        }

        foreach (var result in results)
        {
            if (result.IsSuccess)
            {
                return result;
            }
        }

        foreach (var result in results)
        {
            if (result.IsWarning)
            {
                return result;
            }
        }

        return results[0];
    }

    public static Envelope<T> ToEnvelope<T>(this Result<T> result)
    {
        return new Envelope<T>
        {
            IsSuccess = result.IsSuccess,
            Data = result.IsSuccess ? result.Value : default,
            Code = result.ResultData.DetailCode.Value,
            CodeName = result.ResultData.DetailCode.Name,
            Message = result.ResultData.DetailCode.GetUserFriendlyMessage(result.Message)
        };
    }

    public static Envelope<object?> ToEnvelope(this Result result)
    {
        return new Envelope<object?>
        {
            IsSuccess = result.IsSuccess,
            Data = null,
            Code = result.ResultData.DetailCode.Value,
            CodeName = result.ResultData.DetailCode.Name,
            Message = result.ResultData.DetailCode.GetUserFriendlyMessage(result.Message)
        };
    }
}
