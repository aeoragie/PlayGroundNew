using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Landing;
using PlayGround.Application.Landing.Queries;

namespace PlayGround.Server.Actors
{
    /// <summary>랜딩 콘텐츠 조회 액터. Controller → 이 액터 → 유즈케이스 → 포트 → DB.
    /// 액터는 순수 비동기/동시성 경계이고, 비즈니스는 유즈케이스에 둔다.</summary>
    public sealed class LandingActor : ReceiveActorBase
    {
        public LandingActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetLandingContentsRequest>(HandleGetContentsAsync);
        }

        private async Task HandleGetContentsAsync(GetLandingContentsRequest request)
        {
            // Sender는 await 이후 바뀔 수 있으므로 반드시 먼저 캡처한다 (Akka 함정).
            IActorRef sender = Sender;

            // 액터는 장수(풀), 유즈케이스·레포는 Scoped — 메시지마다 스코프를 열어
            // captive dependency(커넥션 수명 문제)를 피한다.
            using IServiceScope scope = ServiceProvider.CreateScope();
            GetLandingContentsQuery useCase = scope.ServiceProvider.GetRequiredService<GetLandingContentsQuery>();

            Result<LandingContentsResponse> result = await useCase.ExecuteAsync();
            sender.Tell(result);
        }
    }
}
