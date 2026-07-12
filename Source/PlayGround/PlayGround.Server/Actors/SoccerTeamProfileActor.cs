using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Team;
using PlayGround.Application.Team.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>팀 생성 쓰기 액터. Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerTeamProfileActor : ReceiveActorBase
    {
        public SoccerTeamProfileActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<CreateSoccerTeamMessage>(HandleCreateAsync);
        }

        private async Task HandleCreateAsync(CreateSoccerTeamMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            CreateSoccerTeamCommand useCase = scope.ServiceProvider.GetRequiredService<CreateSoccerTeamCommand>();
            Result<CreateTeamResponse> result = await useCase.ExecuteAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }
    }
}
