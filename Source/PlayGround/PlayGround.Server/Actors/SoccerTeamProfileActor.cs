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
            RegisterHandlerAsync<CreateSoccerMatchResultMessage>(HandleCreateMatchResultAsync);
            RegisterHandlerAsync<UpdateSoccerTeamInfoMessage>(HandleUpdateTeamInfoAsync);
            RegisterHandlerAsync<CreateSoccerRecordCorrectionMessage>(HandleCreateCorrectionAsync);
            RegisterHandlerAsync<CancelSoccerRecordCorrectionMessage>(HandleCancelCorrectionAsync);
            RegisterHandlerAsync<SaveSoccerTeamRecruitmentMessage>(HandleSaveRecruitmentAsync);
            RegisterHandlerAsync<CloseSoccerTeamRecruitmentMessage>(HandleCloseRecruitmentAsync);
            RegisterHandlerAsync<DeleteSoccerTeamRecruitmentMessage>(HandleDeleteRecruitmentAsync);
            RegisterHandlerAsync<SaveSoccerScheduleMessage>(HandleSaveScheduleAsync);
            RegisterHandlerAsync<DeleteSoccerScheduleMessage>(HandleDeleteScheduleAsync);
            RegisterHandlerAsync<SaveSoccerTeamCareerOutcomeMessage>(HandleSaveCareerOutcomeAsync);
            RegisterHandlerAsync<DeleteSoccerTeamCareerOutcomeMessage>(HandleDeleteCareerOutcomeAsync);
            RegisterHandlerAsync<SaveSoccerTeamReviewMessage>(HandleSaveReviewAsync);
            RegisterHandlerAsync<DeleteSoccerTeamReviewMessage>(HandleDeleteReviewAsync);
            RegisterHandlerAsync<AddSoccerTeamPlayerMessage>(HandleAddPlayerAsync);
            RegisterHandlerAsync<RemoveSoccerTeamPlayerMessage>(HandleRemovePlayerAsync);
            RegisterHandlerAsync<CreateSoccerApplicationMessage>(HandleCreateApplicationAsync);
            RegisterHandlerAsync<UpdateSoccerApplicationStatusMessage>(HandleUpdateApplicationStatusAsync);
            RegisterHandlerAsync<CancelSoccerApplicationMessage>(HandleCancelApplicationAsync);
            RegisterHandlerAsync<ConfirmSoccerApplicationInviteMessage>(HandleConfirmApplicationInviteAsync);
            RegisterHandlerAsync<SaveSoccerTeamPostMessage>(HandleSavePostAsync);
            RegisterHandlerAsync<SetSoccerTeamPostPinnedMessage>(HandleSetPostPinnedAsync);
            RegisterHandlerAsync<SetSoccerTeamPostPublicMessage>(HandleSetPostPublicAsync);
            RegisterHandlerAsync<DeleteSoccerTeamPostMessage>(HandleDeletePostAsync);
            RegisterHandlerAsync<MarkSoccerTeamPostReadMessage>(HandleMarkPostReadAsync);
        }

        private async Task HandleSavePostAsync(SaveSoccerTeamPostMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamPostCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamPostCommand>();
            Result<TeamPostDto> result = await useCase.SaveAsync(message.ManagerUserId, message.Data, message.AuthorName);
            sender.Tell(result);
        }

        private async Task HandleSetPostPinnedAsync(SetSoccerTeamPostPinnedMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamPostCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamPostCommand>();
            Result<TeamPostDto> result = await useCase.SetPinnedAsync(message.ManagerUserId, message.PostId, message.IsPinned);
            sender.Tell(result);
        }

        private async Task HandleSetPostPublicAsync(SetSoccerTeamPostPublicMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamPostCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamPostCommand>();
            Result<TeamPostDto> result = await useCase.SetPublicAsync(message.ManagerUserId, message.PostId, message.IsPublic);
            sender.Tell(result);
        }

        private async Task HandleDeletePostAsync(DeleteSoccerTeamPostMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamPostCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamPostCommand>();
            Result<bool> result = await useCase.DeleteAsync(message.ManagerUserId, message.PostId, message.Restore);
            sender.Tell(result);
        }

        private async Task HandleMarkPostReadAsync(MarkSoccerTeamPostReadMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamPostCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamPostCommand>();
            Result<bool> result = await useCase.MarkReadAsync(message.UserId, message.PostId);
            sender.Tell(result);
        }

        private async Task HandleConfirmApplicationInviteAsync(ConfirmSoccerApplicationInviteMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerApplicationCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerApplicationCommand>();
            Result<bool> result = await useCase.ConfirmInviteAsync(message.GuardianUserId, message.ApplicationId);
            sender.Tell(result);
        }

        private async Task HandleCreateApplicationAsync(CreateSoccerApplicationMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerApplicationCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerApplicationCommand>();
            Result<Guid> result = await useCase.ApplyAsync(message.GuardianUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleUpdateApplicationStatusAsync(UpdateSoccerApplicationStatusMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerApplicationCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerApplicationCommand>();
            Result<bool> result = await useCase.UpdateStatusAsync(message.ManagerUserId, message.ApplicationId, message.NewStatus);
            sender.Tell(result);
        }

        private async Task HandleCancelApplicationAsync(CancelSoccerApplicationMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerApplicationCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerApplicationCommand>();
            Result<bool> result = await useCase.CancelAsync(message.GuardianUserId, message.ApplicationId);
            sender.Tell(result);
        }

        private async Task HandleAddPlayerAsync(AddSoccerTeamPlayerMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRosterWriteCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRosterWriteCommand>();
            Result<TeamRosterPlayerDto> result = await useCase.AddAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleRemovePlayerAsync(RemoveSoccerTeamPlayerMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRosterWriteCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRosterWriteCommand>();
            Result<bool> result = await useCase.RemoveAsync(message.ManagerUserId, message.TeamPlayerId, message.Restore);
            sender.Tell(result);
        }

        private async Task HandleSaveReviewAsync(SaveSoccerTeamReviewMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamReviewCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamReviewCommand>();
            Result<bool> result = await useCase.SaveAsync(message.AuthorUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleDeleteReviewAsync(DeleteSoccerTeamReviewMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamReviewCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamReviewCommand>();
            Result<bool> result = await useCase.DeleteAsync(message.AuthorUserId, message.ReviewId, message.Restore);
            sender.Tell(result);
        }

        private async Task HandleSaveCareerOutcomeAsync(SaveSoccerTeamCareerOutcomeMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamCareerOutcomeCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamCareerOutcomeCommand>();
            Result<TeamCareerOutcomeDto> result = await useCase.SaveAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleDeleteCareerOutcomeAsync(DeleteSoccerTeamCareerOutcomeMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamCareerOutcomeCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamCareerOutcomeCommand>();
            Result<bool> result = await useCase.DeleteAsync(message.ManagerUserId, message.OutcomeId, message.Restore);
            sender.Tell(result);
        }

        private async Task HandleSaveRecruitmentAsync(SaveSoccerTeamRecruitmentMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRecruitmentCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRecruitmentCommand>();
            Result<TeamRecruitmentDto> result = await useCase.SaveAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleCloseRecruitmentAsync(CloseSoccerTeamRecruitmentMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRecruitmentCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRecruitmentCommand>();
            Result<TeamRecruitmentDto> result = await useCase.CloseAsync(message.ManagerUserId, message.RecruitmentId);
            sender.Tell(result);
        }

        private async Task HandleDeleteRecruitmentAsync(DeleteSoccerTeamRecruitmentMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamRecruitmentCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamRecruitmentCommand>();
            Result<bool> result = await useCase.DeleteAsync(message.ManagerUserId, message.RecruitmentId, message.Restore);
            sender.Tell(result);
        }

        private async Task HandleSaveScheduleAsync(SaveSoccerScheduleMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerScheduleCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerScheduleCommand>();
            Result<ScheduleDto> result = await useCase.SaveAsync(message.UserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleDeleteScheduleAsync(DeleteSoccerScheduleMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerScheduleCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerScheduleCommand>();
            Result<bool> result = await useCase.DeleteAsync(message.UserId, message.ScheduleId, message.Restore);
            sender.Tell(result);
        }

        private async Task HandleCreateCorrectionAsync(CreateSoccerRecordCorrectionMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerRecordCorrectionCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerRecordCorrectionCommand>();
            Result<Guid> result = await useCase.ExecuteAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleCancelCorrectionAsync(CancelSoccerRecordCorrectionMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerRecordCorrectionCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerRecordCorrectionCommand>();
            Result<bool> result = await useCase.CancelAsync(message.ManagerUserId, message.CorrectionId);
            sender.Tell(result);
        }

        private async Task HandleUpdateTeamInfoAsync(UpdateSoccerTeamInfoMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamInfoUpdateCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamInfoUpdateCommand>();
            Result<UpdateTeamInfoResponse> result = await useCase.ExecuteAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleCreateMatchResultAsync(CreateSoccerMatchResultMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamMatchResultCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamMatchResultCommand>();
            Result<CreateTeamMatchResultResponse> result = await useCase.ExecuteAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleCreateAsync(CreateSoccerTeamMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerTeamCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerTeamCommand>();
            Result<CreateTeamResponse> result = await useCase.ExecuteAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }
    }
}
