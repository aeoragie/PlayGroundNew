using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Player;
using PlayGround.Application.Player.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 쓰기 액터. Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerPlayerProfileActor : ReceiveActorBase
    {
        public SoccerPlayerProfileActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<CreatePlayerProfileMessage>(HandleCreateAsync);
        }

        private async Task HandleCreateAsync(CreatePlayerProfileMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            CreatePlayerProfileCommand useCase = scope.ServiceProvider.GetRequiredService<CreatePlayerProfileCommand>();

            Result<CreatePlayerProfileResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data);
            sender.Tell(result);
        }
    }
}
