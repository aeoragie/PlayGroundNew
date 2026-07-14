using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Team;
using PlayGround.Application.Team.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>팀 대시보드 읽기 액터 (팀 정보·선수단). Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerTeamInfoActor : ReceiveActorBase
    {
        public SoccerTeamInfoActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetSoccerTeamInfoMessage>(HandleGetInfoAsync);
            RegisterHandlerAsync<GetSoccerTeamRosterMessage>(HandleGetRosterAsync);
            RegisterHandlerAsync<GetSoccerTeamHomeMessage>(HandleGetHomeAsync);
        }

        private async Task HandleGetInfoAsync(GetSoccerTeamInfoMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamInfoCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamInfoCommand>();
            Result<TeamInfoResponse> result = await useCase.ExecuteAsync(message.ManagerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetRosterAsync(GetSoccerTeamRosterMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRosterCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRosterCommand>();
            Result<TeamRosterResponse> result = await useCase.ExecuteAsync(message.ManagerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetHomeAsync(GetSoccerTeamHomeMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamPublicHomeCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamPublicHomeCommand>();
            Result<TeamPublicHomeResponse> result = await useCase.ExecuteAsync(message.Slug);
            sender.Tell(result);
        }
    }
}
