using System.Diagnostics;

namespace PlayGround.Shared.Result
{
    public class ResultBuilder<T>
    {
        private T? mValue;
        private DetailCode? mDetailCode;
        private string? mMessage;
        private string? mDetails;
        private Exception? mException;

        private readonly Dictionary<string, object> Metadatas = new();
        private readonly Stopwatch Stopwatch = new();

        public ResultBuilder()
        {
            Stopwatch.Start();
        }

        #region Value Setting

        public ResultBuilder<T> WithValue(T value)
        {
            mValue = value;
            return this;
        }

        public ResultBuilder<T> WithValueIf(bool condition, T value)
        {
            if (condition)
            {
                mValue = value;
            }
            return this;
        }

        public ResultBuilder<T> WithValue(Func<T> factory)
        {
            try
            {
                mValue = factory();
            }
            catch (Exception ex)
            {
                return WithException(ex);
            }
            return this;
        }

        #endregion

        #region Error Handling

        public ResultBuilder<T> WithError(ErrorCode errorCode, string? message = null, string? details = null)
        {
            mDetailCode = errorCode;
            mMessage = message;
            mDetails = details;
            return this;
        }

        public ResultBuilder<T> WithErrorIf(bool condition, ErrorCode errorCode, string? message = null)
        {
            if (condition)
            {
                return WithError(errorCode, message);
            }
            return this;
        }

        public ResultBuilder<T> WithException(Exception exception, ErrorCode? errorCode = null)
        {
            mException = exception;
            mDetailCode = errorCode ?? MapExceptionToErrorCode(exception);
            mMessage = exception.Message;
            mDetails = exception.StackTrace;
            return this;
        }

        #endregion

        #region Warning & Information

        public ResultBuilder<T> WithWarning(WarningCode warningCode, string? message = null, string? details = null)
        {
            mDetailCode = warningCode;
            mMessage = message;
            mDetails = details;
            return this;
        }

        public ResultBuilder<T> WithInformation(InformationCode informationCode, string? message = null, string? details = null)
        {
            mDetailCode = informationCode;
            mMessage = message;
            mDetails = details;
            return this;
        }

        #endregion

        #region Metadata & Context

        public ResultBuilder<T> WithMetadata(string key, object value)
        {
            Metadatas[key] = value;
            return this;
        }

        public ResultBuilder<T> WithCorrelationId(string correlationId)
        {
            return WithMetadata("CorrelationId", correlationId);
        }

        public ResultBuilder<T> WithAffectedRows(int affectedRows)
        {
            return WithMetadata("AffectedRows", affectedRows);
        }

        public ResultBuilder<T> WithDetails(string details)
        {
            mDetails = details;
            return this;
        }

        #endregion

        #region Validation

        public ResultBuilder<T> ValidateValue(Func<T?, bool> validator, ErrorCode errorCode, string? message = null)
        {
            if (mValue != null && !validator(mValue))
            {
                return WithError(errorCode, message);
            }
            return this;
        }

        public ResultBuilder<T> EnsureNotNull(ErrorCode? errorCode = null, string? message = null)
        {
            if (mValue == null)
            {
                return WithError(errorCode ?? ErrorCode.MissingRequired, message ?? "Value cannot be null");
            }
            return this;
        }

        #endregion

        #region Build Methods

        public Result<T> Build()
        {
            Stopwatch.Stop();

            // 실행 시간을 메타데이터에 추가
            WithMetadata("ExecutionTime", Stopwatch.Elapsed);

            // 에러가 설정된 경우
            if (mDetailCode?.IsError == true)
            {
                var resultInfo = mException != null
                    ? ResultInfo.Exception(mException, (ErrorCode)mDetailCode)
                    : ResultInfo.Error((ErrorCode)mDetailCode, mMessage, mDetails);

                return Result<T>.Failure(resultInfo);
            }

            // 경고가 설정된 경우 (값과 함께 반환)
            if (mDetailCode?.IsWarning == true && mValue != null)
            {
                return Result<T>.Warning(mValue, (WarningCode)mDetailCode, mMessage, mDetails);
            }

            // 정보가 설정된 경우 (값과 함께 반환)
            if (mDetailCode?.IsInformation == true && mValue != null)
            {
                return Result<T>.Information(mValue, (InformationCode)mDetailCode, mMessage, mDetails);
            }

            // 값이 있는 경우 성공
            if (mValue != null)
            {
                return Result<T>.Success(mValue);
            }

            return Result<T>.Unknown();
        }

        public async Task<Result<T>> BuildAsync()
        {
            return await Task.FromResult(Build());
        }

        public Result<T> BuildIf(bool condition, Func<Result<T>>? alternativeBuilder = null)
        {
            if (condition)
            {
                return Build();
            }
            return alternativeBuilder?.Invoke() ?? Result<T>.Unknown();
        }

        #endregion

        #region Utility Methods

        private static ErrorCode MapExceptionToErrorCode(Exception exception)
        {
            return exception switch
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

        public override string ToString()
        {
            var status = mDetailCode?.Category.ToString() ?? "Building";
            var valueInfo = mValue != null ? $"Value: {mValue}" : "No Value";
            var metadataInfo = Metadatas.Count > 0 ? $"Metadata: {Metadatas.Count} items" : "No Metadata";

            return $"ResultBuilder<{typeof(T).Name}> [{status}] - {valueInfo}, {metadataInfo}";
        }

        #endregion
    }

    //.// ResultBuilder

    public class ResultBuilder
    {
        private DetailCode? mDetailCode;
        private string? mMessage;
        private string? mDetails;
        private Exception? mException;
        private readonly Dictionary<string, object> Metadatas = new();
        private readonly Stopwatch Stopwatch = new();

        public ResultBuilder()
        {
            Stopwatch.Start();
        }

        #region Error Handling

        public ResultBuilder WithError(ErrorCode errorCode, string? message = null, string? details = null)
        {
            mDetailCode = errorCode;
            mMessage = message;
            mDetails = details;
            return this;
        }

        public ResultBuilder WithErrorIf(bool condition, ErrorCode errorCode, string? message = null)
        {
            if (condition)
            {
                return WithError(errorCode, message);
            }
            return this;
        }

        public ResultBuilder WithException(Exception exception, ErrorCode? errorCode = null)
        {
            mException = exception;
            mDetailCode = errorCode ?? MapExceptionToErrorCode(exception);
            mMessage = exception.Message;
            mDetails = exception.StackTrace;
            return this;
        }

        #endregion

        #region Warning & Information

        public ResultBuilder WithWarning(WarningCode warningCode, string? message = null, string? details = null)
        {
            mDetailCode = warningCode;
            mMessage = message;
            mDetails = details;
            return this;
        }

        public ResultBuilder WithInfo(InformationCode infoCode, string? message = null, string? details = null)
        {
            mDetailCode = infoCode;
            mMessage = message;
            mDetails = details;
            return this;
        }

        #endregion

        #region Metadata

        public ResultBuilder WithMetadata(string key, object value)
        {
            Metadatas[key] = value;
            return this;
        }

        public ResultBuilder WithCorrelationId(string correlationId)
        {
            return WithMetadata("CorrelationId", correlationId);
        }

        public ResultBuilder WithDetails(string details)
        {
            mDetails = details;
            return this;
        }

        #endregion

        #region Build Methods

        public Result Build()
        {
            Stopwatch.Stop();
            WithMetadata("ExecutionTime", Stopwatch.Elapsed);

            if (mDetailCode?.IsError == true)
            {
                return mException != null
                    ? Result.FromException(mException, (ErrorCode)mDetailCode)
                    : Result.Error((ErrorCode)mDetailCode, mMessage, mDetails);
            }

            if (mDetailCode?.IsWarning == true)
            {
                return Result.Warning((WarningCode)mDetailCode, mMessage, mDetails);
            }

            if (mDetailCode?.IsInformation == true)
            {
                return Result.Information((InformationCode)mDetailCode, mMessage, mDetails);
            }

            return Result.Success();
        }

        public async Task<Result> BuildAsync()
        {
            return await Task.FromResult(Build());
        }

        #endregion

        private static ErrorCode MapExceptionToErrorCode(Exception exception) => exception switch
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
}
