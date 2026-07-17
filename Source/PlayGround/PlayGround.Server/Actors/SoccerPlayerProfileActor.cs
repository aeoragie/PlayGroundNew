using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Player;
using PlayGround.Application.Player.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>선수 프로필 액터 (생성·조회·공개 설정). Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class SoccerPlayerProfileActor : ReceiveActorBase
    {
        public SoccerPlayerProfileActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<CreatePlayerProfileMessage>(HandleCreateAsync);
            RegisterHandlerAsync<GetSoccerPlayerInfoMessage>(HandleGetInfoAsync);
            RegisterHandlerAsync<SetSoccerPlayerFieldVisibilityMessage>(HandleSetVisibilityAsync);
            RegisterHandlerAsync<ClaimSoccerPlayerInviteMessage>(HandleClaimAsync);
            RegisterHandlerAsync<GetSoccerPlayerCareerMessage>(HandleGetCareerAsync);
            RegisterHandlerAsync<GetSoccerPlayerPortfolioMessage>(HandleGetPortfolioAsync);
            RegisterHandlerAsync<GetSoccerPlayerSeasonStatsMessage>(HandleGetSeasonStatsAsync);
        }

        private async Task HandleCreateAsync(CreatePlayerProfileMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerProfileCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerProfileCommand>();

            Result<CreatePlayerProfileResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleGetInfoAsync(GetSoccerPlayerInfoMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerInfoCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerInfoCommand>();
            Result<PlayerInfoResponse> result = await useCase.ExecuteAsync(message.UserId);
            sender.Tell(result);
        }

        private async Task HandleSetVisibilityAsync(SetSoccerPlayerFieldVisibilityMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerFieldVisibilityCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerFieldVisibilityCommand>();
            Result<bool> result = await useCase.ExecuteAsync(message.UserId, message.Data.FieldName, message.Data.IsPublic);
            sender.Tell(result);
        }

        private async Task HandleClaimAsync(ClaimSoccerPlayerInviteMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerClaimCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerClaimCommand>();
            Result<ClaimPlayerInviteResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data.Code, message.CurrentRole);
            sender.Tell(result);
        }

        private async Task HandleGetCareerAsync(GetSoccerPlayerCareerMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerCareerCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerCareerCommand>();
            Result<PlayerCareerResponse> result = await useCase.ExecuteAsync(message.UserId);
            sender.Tell(result);
        }

        private async Task HandleGetPortfolioAsync(GetSoccerPlayerPortfolioMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerPortfolioCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerPortfolioCommand>();
            Result<PlayerPortfolioResponse> result = await useCase.ExecuteAsync(message.UserId);
            sender.Tell(result);
        }

        private async Task HandleGetSeasonStatsAsync(GetSoccerPlayerSeasonStatsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerPlayerSeasonStatsCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerPlayerSeasonStatsCommand>();
            Result<PlayerSeasonStatsResponse> result = await useCase.ExecuteAsync(message.UserId, message.SeasonYear);
            sender.Tell(result);
        }
    }
}
