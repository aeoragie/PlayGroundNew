using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Team;
using PlayGround.Application.Team.Commands;

namespace PlayGround.Server.Actors
{
    public sealed class SoccerDashboardActor : ReceiveActorBase
    {
        public SoccerDashboardActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetSoccerDashboardHubMessage>(HandleGetHubAsync);
        }

        private async Task HandleGetHubAsync(GetSoccerDashboardHubMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerDashboardHubCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerDashboardHubCommand>();
            Result<DashboardHubResponse> result =
                await useCase.ExecuteAsync(message.UserId, message.DisplayName, message.SeasonYear);
            sender.Tell(result);
        }
    }

    public sealed record GetSoccerDashboardHubMessage(Guid UserId, string DisplayName, int SeasonYear);
}
