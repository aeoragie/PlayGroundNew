using Akka.Actor;
using PlayGround.Application.Team.Commands;
using PlayGround.Contracts.Team;
using PlayGround.Infrastructure.Actor;
using PlayGround.Shared.Result;

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
