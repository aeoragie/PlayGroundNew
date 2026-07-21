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
