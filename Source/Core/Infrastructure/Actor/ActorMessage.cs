using Akka.Routing;

namespace PlayGround.Infrastructure.Actor
{
    public enum ActorResultCode
    {
        Success = 0,
        Error,
        Timeout,
        ResultDataNull,
    }

    public class ActorMessage : IConsistentHashable
    {
        public ActorResultCode ResultCode { get; set; } = ActorResultCode.Success;
        public string ResultMessage { get; set; } = string.Empty;
        public object? ConsistentHashKey { get; set; }

        public bool IsSuccess => ResultCode == ActorResultCode.Success;

        public ActorMessage SetResultCode(ActorResultCode code)
        {
            ResultCode = code;
            return this;
        }

        public ActorMessage SetResult(ActorResultCode code, string message = "")
        {
            ResultCode = code;
            ResultMessage = message;
            return this;
        }
    }

    public class ActorMessage<TRequest> : ActorMessage
    {
        public TRequest? RequestData { get; set; }

        public new ActorMessage<TRequest> SetResultCode(ActorResultCode code)
        {
            ResultCode = code;
            return this;
        }
    }

    public class ActorMessage<TRequest, TResult> : ActorMessage<TRequest>
    {
        public TResult? ResultData { get; set; }

        public new ActorMessage<TRequest, TResult> SetResultCode(ActorResultCode code)
        {
            ResultCode = code;
            return this;
        }

        public new ActorMessage<TRequest, TResult> SetResult(ActorResultCode code, string message = "")
        {
            ResultCode = code;
            ResultMessage = message;
            return this;
        }
    }
}
