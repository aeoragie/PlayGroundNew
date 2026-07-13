using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Team;
using PlayGround.Application.Team.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>팀 정보 조회 읽기 액터. Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerTeamInfoActor : ReceiveActorBase
    {
        public SoccerTeamInfoActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetSoccerTeamInfoMessage>(HandleGetInfoAsync);
        }

        private async Task HandleGetInfoAsync(GetSoccerTeamInfoMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamInfoCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamInfoCommand>();
            Result<TeamInfoResponse> result = await useCase.ExecuteAsync(message.ManagerUserId);
            sender.Tell(result);
        }
    }
}
