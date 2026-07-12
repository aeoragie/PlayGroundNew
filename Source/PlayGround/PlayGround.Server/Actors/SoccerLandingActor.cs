using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Landing;
using PlayGround.Application.Landing.Queries;

namespace PlayGround.Server.Actors
{
    /// <summary>랜딩 콘텐츠 조회 액터. Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerLandingActor : ReceiveActorBase
    {
        public SoccerLandingActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetLandingContentsRequest>(HandleGetContentsAsync);
        }

        private async Task HandleGetContentsAsync(GetLandingContentsRequest request)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            GetLandingContentsQuery useCase = scope.ServiceProvider.GetRequiredService<GetLandingContentsQuery>();
            Result<LandingContentsResponse> result = await useCase.ExecuteAsync();
            sender.Tell(result);
        }
    }
}
