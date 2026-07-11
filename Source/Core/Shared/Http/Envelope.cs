using PlayGround.Shared.Result;

namespace PlayGround.Shared.Http
{
    public class Envelope<T>
    {
        public bool IsSuccess { get; init; }
        public T? Data { get; init; }
        public int Code { get; init; }
        public string CodeName { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;

        public static Envelope<T> Success(T data, int code, string codeName, string message)
        {
            return new Envelope<T>
            {
                IsSuccess = true,
                Data = data,
                Code = code,
                CodeName = codeName,
                Message = message
            };
        }

        /// <summary>
        /// 기존 코드 호환용 - SuccessCode.Ok 기본값 사용
        /// </summary>
        public static Envelope<T> Success(T data)
        {
            return new Envelope<T>
            {
                IsSuccess = true,
                Data = data,
                Code = SuccessCode.Ok.Value,
                CodeName = SuccessCode.Ok.Name,
                Message = SuccessCode.Ok.DefaultMessage
            };
        }

        public static Envelope<T> Fail(int code, string codeName, string message)
        {
            return new Envelope<T>
            {
                IsSuccess = false,
                Data = default,
                Code = code,
                CodeName = codeName,
                Message = message
            };
        }

        /// <summary>
        /// 기존 코드 호환용 - ErrorCode.UnknownError 기본값 사용
        /// </summary>
        public static Envelope<T> Fail(string message)
        {
            return new Envelope<T>
            {
                IsSuccess = false,
                Data = default,
                Code = ErrorCode.UnknownError.Value,
                CodeName = ErrorCode.UnknownError.Name,
                Message = message
            };
        }
    }
}
