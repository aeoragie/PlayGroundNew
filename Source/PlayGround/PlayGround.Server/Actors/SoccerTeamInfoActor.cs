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
            RegisterHandlerAsync<GetSoccerTeamMatchesMessage>(HandleGetMatchesAsync);
            RegisterHandlerAsync<GetSoccerTeamVideosMessage>(HandleGetVideosAsync);
            RegisterHandlerAsync<GetSoccerTeamSeasonRecordMessage>(HandleGetSeasonRecordAsync);
            RegisterHandlerAsync<GetSoccerTournamentOptionsMessage>(HandleGetTournamentOptionsAsync);
            RegisterHandlerAsync<GetSoccerRecordCorrectionsMessage>(HandleGetCorrectionsAsync);
            RegisterHandlerAsync<GetSoccerActionItemsMessage>(HandleGetActionItemsAsync);
            RegisterHandlerAsync<GetSoccerTeamExploreMessage>(HandleGetExploreAsync);
            RegisterHandlerAsync<GetSoccerTeamRecruitmentsMessage>(HandleGetRecruitmentsAsync);
            RegisterHandlerAsync<GetSoccerTeamRecruitmentsByManagerMessage>(HandleGetRecruitmentsByManagerAsync);
            RegisterHandlerAsync<GetSoccerTeamCareerOutcomesMessage>(HandleGetCareerOutcomesAsync);
            RegisterHandlerAsync<GetSoccerTeamCareerOutcomesByManagerMessage>(HandleGetCareerOutcomesByManagerAsync);
            RegisterHandlerAsync<GetSoccerTeamReviewsMessage>(HandleGetReviewsAsync);
        }

        private async Task HandleGetReviewsAsync(GetSoccerTeamReviewsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamReviewCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamReviewCommand>();
            Result<TeamReviewsResponse> result = await useCase.GetBySlugAsync(message.Slug, message.ViewerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetCareerOutcomesAsync(GetSoccerTeamCareerOutcomesMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamCareerOutcomeCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamCareerOutcomeCommand>();
            Result<TeamCareerOutcomesResponse> result = await useCase.GetBySlugAsync(message.Slug);
            sender.Tell(result);
        }

        private async Task HandleGetCareerOutcomesByManagerAsync(GetSoccerTeamCareerOutcomesByManagerMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamCareerOutcomeCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamCareerOutcomeCommand>();
            Result<TeamCareerOutcomesResponse> result = await useCase.GetMineAsync(message.ManagerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetRecruitmentsAsync(GetSoccerTeamRecruitmentsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRecruitmentCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRecruitmentCommand>();
            Result<TeamRecruitmentsResponse> result = await useCase.GetBySlugAsync(message.Slug);
            sender.Tell(result);
        }

        private async Task HandleGetRecruitmentsByManagerAsync(GetSoccerTeamRecruitmentsByManagerMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRecruitmentCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRecruitmentCommand>();
            Result<TeamRecruitmentsResponse> result = await useCase.GetMineAsync(message.ManagerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetExploreAsync(GetSoccerTeamExploreMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamExploreCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamExploreCommand>();
            Result<TeamExploreResponse> result = await useCase.ExecuteAsync();
            sender.Tell(result);
        }

        private async Task HandleGetActionItemsAsync(GetSoccerActionItemsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerActionItemsCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerActionItemsCommand>();
            Result<ActionItemsResponse> result = await useCase.ExecuteAsync(message.UserId);
            sender.Tell(result);
        }

        private async Task HandleGetCorrectionsAsync(GetSoccerRecordCorrectionsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerRecordCorrectionCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerRecordCorrectionCommand>();
            Result<RecordCorrectionsResponse> result = await useCase.GetAsync(message.ManagerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetTournamentOptionsAsync(GetSoccerTournamentOptionsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamMatchResultCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamMatchResultCommand>();
            Result<TeamTournamentOptionsResponse> result =
                await useCase.GetTournamentOptionsAsync(message.ManagerUserId, message.SeasonYear);
            sender.Tell(result);
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
            Result<TeamPublicHomeResponse> result = await useCase.ExecuteAsync(message.Slug, message.ViewerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetMatchesAsync(GetSoccerTeamMatchesMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamMatchesCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamMatchesCommand>();
            Result<TeamMatchesResponse> result = await useCase.ExecuteAsync(message.ManagerUserId, message.SeasonYear);
            sender.Tell(result);
        }

        private async Task HandleGetVideosAsync(GetSoccerTeamVideosMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamVideosCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamVideosCommand>();
            Result<TeamVideosResponse> result = await useCase.ExecuteAsync(message.ManagerUserId);
            sender.Tell(result);
        }

        private async Task HandleGetSeasonRecordAsync(GetSoccerTeamSeasonRecordMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamSeasonRecordCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamSeasonRecordCommand>();
            Result<TeamSeasonRecordResponse> result = await useCase.ExecuteAsync(message.Slug, message.SeasonYear);
            sender.Tell(result);
        }
    }
}
