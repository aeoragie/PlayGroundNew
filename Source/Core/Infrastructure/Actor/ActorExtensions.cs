using Akka.Actor;

namespace PlayGround.Infrastructure.Actor
{
    /// <summary>
    /// IActorRef 확장 메서드 (Response, SendAsync, Verify)
    /// </summary>
    public static class ActorExtensions
    {
        #region Response (액터 내부에서 Sender에게 응답)

        public static void Response(this IActorRef sender, ActorMessage message)
        {
            sender.Tell(message);
        }

        #endregion

        #region SendAsync (Ask 패턴 래핑, 타임아웃 시 Timeout 코드 반환)

        public static async Task<ActorMessage> SendAsync(this IActorRef actor, ActorMessage message, TimeSpan? timeout = null)
        {
            try
            {
                var result = await actor.Ask<ActorMessage>(message, timeout);
                return result ?? message.SetResultCode(ActorResultCode.Error);
            }
            catch (AskTimeoutException)
            {
                return message.SetResultCode(ActorResultCode.Timeout);
            }
        }

        public static async Task<ActorMessage<TRequest>> SendAsync<TRequest>(
            this IActorRef actor, ActorMessage<TRequest> message, TimeSpan? timeout = null)
        {
            try
            {
                var result = await actor.Ask<ActorMessage<TRequest>>(message, timeout);
                return result ?? message.SetResultCode(ActorResultCode.Error);
            }
            catch (AskTimeoutException)
            {
                return message.SetResultCode(ActorResultCode.Timeout);
            }
        }

        public static async Task<ActorMessage<TRequest, TResult>> SendAsync<TRequest, TResult>(
            this IActorRef actor, ActorMessage<TRequest, TResult> message, TimeSpan? timeout = null)
            where TResult : class, new()
        {
            try
            {
                var result = await actor.Ask<ActorMessage<TRequest, TResult>>(message, timeout);
                if (result == null)
                {
                    return message.SetResultCode(ActorResultCode.Error);
                }

                if (result.ResultData == null)
                {
                    result.ResultData = new TResult();
                    return result.SetResultCode(ActorResultCode.ResultDataNull);
                }

                return result;
            }
            catch (AskTimeoutException)
            {
                return message.SetResultCode(ActorResultCode.Timeout);
            }
        }

        #endregion

        #region Verify (메시지 검증)

        public static bool Verify<T>(this T? message) where T : ActorMessage
        {
            return message != null;
        }

        public static (bool Verified, TRequest Request) Verify<TRequest>(
            this ActorMessage<TRequest>? message) where TRequest : class, new()
        {
            if (message?.RequestData == null)
            {
                return (false, new TRequest());
            }
            return (true, message.RequestData);
        }

        #endregion
    }
}
