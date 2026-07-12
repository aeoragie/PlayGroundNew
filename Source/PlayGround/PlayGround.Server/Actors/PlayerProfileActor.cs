using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Player;
using PlayGround.Application.Player.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 쓰기 액터. ConsistentHash 라우터(UserId 키)로 사용자별 순차 처리.
    /// 액터는 비동기/직렬화 경계이고, 검증·저장은 유즈케이스에 둔다.</summary>
    public sealed class PlayerProfileActor : ReceiveActorBase
    {
        public PlayerProfileActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<CreatePlayerProfileMessage>(HandleCreateAsync);
        }

        private async Task HandleCreateAsync(CreatePlayerProfileMessage message)
        {
            // Sender는 await 이후 바뀔 수 있으므로 먼저 캡처.
            IActorRef sender = Sender;

            // 메시지마다 스코프 — Scoped 유즈케이스/레포의 커넥션 수명 안전 확보.
            using IServiceScope scope = ServiceProvider.CreateScope();
            CreatePlayerProfileCommand useCase = scope.ServiceProvider.GetRequiredService<CreatePlayerProfileCommand>();

            Result<CreatePlayerProfileResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data);
            sender.Tell(result);
        }
    }
}
