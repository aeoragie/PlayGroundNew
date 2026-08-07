using PlayGround.Shared.Http;
using PlayGround.Shared.Time;

namespace PlayGround.Shared.Result;

public static class ResultExtensions
{

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
