using Akka.Actor;
using Microsoft.Extensions.DependencyInjection;
using PlayGround.Shared.Result;
using PlayGround.Infrastructure.Actor;
using PlayGround.Contracts.Agent;
using PlayGround.Contracts.Claim;
using PlayGround.Contracts.Notification;
using PlayGround.Application.Agent.Commands;
using PlayGround.Application.Claim.Commands;
using PlayGround.Application.Notification.Commands;

namespace PlayGround.Server.Actors
{
    /// <summary>Claim 플로우 + 알림 센터 액터 (UserId 해시 — 요청 생성·승인·읽음의 사용자별 순차).</summary>
    public sealed class SoccerClaimActor : ReceiveActorBase
    {
        public SoccerClaimActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<GetClaimInviteCardMessage>(HandleLookupAsync);
            RegisterHandlerAsync<CreateClaimRequestMessage>(HandleCreateAsync);
            RegisterHandlerAsync<GetOwnClaimRequestMessage>(HandleGetOwnAsync);
            RegisterHandlerAsync<ReviewClaimRequestMessage>(HandleReviewAsync);
            RegisterHandlerAsync<GetNotificationsMessage>(HandleGetNotificationsAsync);
            RegisterHandlerAsync<MarkNotificationReadMessage>(HandleMarkReadAsync);
            RegisterHandlerAsync<GetAgentViewRequestMessage>(HandleGetAgentRequestAsync);
            RegisterHandlerAsync<ReviewAgentViewRequestMessage>(HandleReviewAgentRequestAsync);
            RegisterHandlerAsync<BlockAgentMessage>(HandleBlockAgentAsync);
        }

        private async Task HandleGetAgentRequestAsync(GetAgentViewRequestMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerAgentApprovalCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerAgentApprovalCommand>();
            Result<AgentViewRequestResponse> result = await useCase.GetAsync(message.UserId, message.RequestId);
            sender.Tell(result);
        }

        private async Task HandleReviewAgentRequestAsync(ReviewAgentViewRequestMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerAgentApprovalCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerAgentApprovalCommand>();
            Result<AgentViewRequestResponse> result = await useCase.ReviewAsync(message.UserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleBlockAgentAsync(BlockAgentMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerAgentApprovalCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerAgentApprovalCommand>();
            Result<bool> result = await useCase.BlockAsync(message.UserId, message.RequestId);
            sender.Tell(result);
        }

        private async Task HandleLookupAsync(GetClaimInviteCardMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerClaimFlowCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerClaimFlowCommand>();
            Result<ClaimInviteCardResponse> result = await useCase.LookupAsync(message.Code);
            sender.Tell(result);
        }

        private async Task HandleCreateAsync(CreateClaimRequestMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerClaimFlowCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerClaimFlowCommand>();
            Result<ClaimRequestSummaryResponse> result = await useCase.CreateAsync(message.UserId, message.RequesterName, message.Data);
            sender.Tell(result);
        }

        private async Task HandleGetOwnAsync(GetOwnClaimRequestMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerClaimFlowCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerClaimFlowCommand>();
            Result<ClaimRequestSummaryResponse> result = await useCase.GetMineAsync(message.UserId);
            sender.Tell(result);
        }

        private async Task HandleReviewAsync(ReviewClaimRequestMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerClaimReviewCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerClaimReviewCommand>();
            Result<ReviewClaimResponse> result = await useCase.ExecuteAsync(message.ManagerUserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleGetNotificationsAsync(GetNotificationsMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerNotificationCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerNotificationCommand>();
            Result<NotificationsResponse> result = await useCase.GetAsync(message.UserId);
            sender.Tell(result);
        }

        private async Task HandleMarkReadAsync(MarkNotificationReadMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            SoccerNotificationCommand useCase = scope.ServiceProvider.GetRequiredService<SoccerNotificationCommand>();
            Result<bool> result = await useCase.MarkReadAsync(message.UserId, message.NotificationId);
            sender.Tell(result);
        }
    }
}
