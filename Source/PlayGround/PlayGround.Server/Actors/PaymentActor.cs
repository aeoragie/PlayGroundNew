using Akka.Actor;
using PlayGround.Application.Payment.Commands;
using PlayGround.Contracts.Payment;
using PlayGround.Infrastructure.Actor;
using PlayGround.Shared.Result;

namespace PlayGround.Server.Actors
{
    /// <summary>결제 쓰기 액터 (종목 공통). Controller → 액터 → 유즈케이스 → DB.</summary>
    public sealed class PaymentActor : ReceiveActorBase
    {
        public PaymentActor(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            RegisterHandlerAsync<CreatePaymentOrderMessage>(HandleCreateOrderAsync);
            RegisterHandlerAsync<ConfirmPaymentMessage>(HandleConfirmAsync);
            RegisterHandlerAsync<FailPaymentMessage>(HandleFailAsync);
        }

        private async Task HandleCreateOrderAsync(CreatePaymentOrderMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            PaymentOrderCommand useCase = scope.ServiceProvider.GetRequiredService<PaymentOrderCommand>();
            Result<PaymentSummaryResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleConfirmAsync(ConfirmPaymentMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            PaymentConfirmCommand useCase = scope.ServiceProvider.GetRequiredService<PaymentConfirmCommand>();
            Result<PaymentSummaryResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data);
            sender.Tell(result);
        }

        private async Task HandleFailAsync(FailPaymentMessage message)
        {
            IActorRef sender = Sender; // await 전에 캡처 (Akka Sender 함정)
            using IServiceScope scope = ServiceProvider.CreateScope();
            PaymentFailCommand useCase = scope.ServiceProvider.GetRequiredService<PaymentFailCommand>();
            Result<PaymentSummaryResponse> result = await useCase.ExecuteAsync(message.UserId, message.Data);
            sender.Tell(result);
        }
    }
}
