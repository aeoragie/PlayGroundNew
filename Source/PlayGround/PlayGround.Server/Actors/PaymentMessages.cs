using Akka.Routing;
using PlayGround.Contracts.Payment;

namespace PlayGround.Server.Actors
{
    /// <summary>주문 생성 메시지 (쓰기 — UserId 해시. 중복 제출 경합 방지).</summary>
    public sealed record CreatePaymentOrderMessage(Guid UserId, CreatePaymentOrderRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>결제 승인 메시지 (쓰기 — UserId 해시. 복귀 페이지 중복 confirm을 순차 처리로 보강).</summary>
    public sealed record ConfirmPaymentMessage(Guid UserId, ConfirmPaymentRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }

    /// <summary>결제 실패 기록 메시지 (쓰기 — UserId 해시).</summary>
    public sealed record FailPaymentMessage(Guid UserId, FailPaymentRequest Data) : IConsistentHashable
    {
        public object ConsistentHashKey => UserId;
    }
}
