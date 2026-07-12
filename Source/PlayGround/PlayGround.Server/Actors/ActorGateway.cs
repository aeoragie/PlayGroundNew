using System.Diagnostics;
using Akka.Actor;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;

namespace PlayGround.Server.Actors
{
    /// <summary>컨트롤러가 액터에 요청-응답하는 얇은 파사드. Result&lt;T&gt;를 메일박스로 그대로 왕복시킨다.</summary>
    public sealed class ActorGateway
    {
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);

        private readonly AkkaService mAkka;

        public ActorGateway(AkkaService akka)
        {
            ArgumentNullException.ThrowIfNull(akka);
            Debug.Assert(akka is not null);
            mAkka = akka;
        }

        public async Task<Result<TResponse>> AskAsync<TResponse>(
            string actorName,
            object request,
            CancellationToken cancellation = default,
            TimeSpan? timeout = null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(actorName);
            ArgumentNullException.ThrowIfNull(request);

            ActorRef? actor = mAkka.GetActor(actorName);
            if (actor is null)
            {
                return Result<TResponse>.Error(ErrorCode.ServiceUnavailable, $"Actor not available: {actorName}");
            }

            try
            {
                return await actor.Raw.Ask<Result<TResponse>>(request, timeout ?? DefaultTimeout, cancellation);
            }
            catch (AskTimeoutException)
            {
                return Result<TResponse>.Error(ErrorCode.MessageTimeout, $"Actor request timed out: {actorName}");
            }
        }
    }
}
