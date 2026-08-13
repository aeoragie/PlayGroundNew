using Microsoft.Extensions.Options;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Models;
using PlayGround.Contracts.Payment;
using PlayGround.Domain.Payment;
using PlayGround.Infrastructure.Database;
using PlayGround.Infrastructure.Database.Base;
using PlayGround.Persistence.Database.Generated.Soccer.Entities;
using PlayGround.Persistence.Database.Generated.Soccer.Procedures;
using PlayGround.Shared.Extensions;
using PlayGround.Shared.Result;

namespace PlayGround.Persistence.Repositories
{
    /// <summary>결제 원장 (종목 공통, 물리 DB는 Soccer). 상태 조건 불일치는 빈 결과셋이라 null로 해석된다.</summary>
    public class PaymentRepository : RepositoryBase, IPaymentRepository
    {
        public override DatabaseTypes Database => DatabaseTypes.Soccer;

        public PaymentRepository(IOptions<DatabaseConfiguration> options) : base(options)
        {
        }

        public async Task<Result<PaymentSummaryResponse?>> CreateAsync(CreatePaymentInput input, CancellationToken cancellation = default)
        {
            var procedure = new UspCreatePayment(this)
            {
                UserId = input.UserId,
                Sport = input.Sport.ToString(),
                OrderId = input.OrderId,
                OrderName = input.OrderName,
                Amount = input.Amount,
                Currency = input.Currency,
                PgProvider = input.PgProvider.ToString()
            };

            return await QuerySingleAsync(procedure, cancellation);
        }

        public async Task<Result<PaymentSummaryResponse?>> FindByOrderAsync(Guid userId, string orderId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetPaymentByOrder(this) { UserId = userId, OrderId = orderId };
            return await QuerySingleAsync(procedure, cancellation);
        }

        public async Task<Result<PaymentSummaryResponse?>> ApproveAsync(ApprovePaymentInput input, CancellationToken cancellation = default)
        {
            var procedure = new UspApprovePayment(this)
            {
                UserId = input.UserId,
                OrderId = input.OrderId,
                PaymentKey = input.PaymentKey,
                Method = input.Method!,
                ApprovedAt = input.ApprovedAt
            };

            return await QuerySingleAsync(procedure, cancellation);
        }

        public async Task<Result<PaymentSummaryResponse?>> FailAsync(Guid userId, string orderId, string failReason, CancellationToken cancellation = default)
        {
            var procedure = new UspFailPayment(this) { UserId = userId, OrderId = orderId, FailReason = failReason };
            return await QuerySingleAsync(procedure, cancellation);
        }

        public async Task<Result<List<PaymentSummaryResponse>>> ListByUserAsync(Guid userId, CancellationToken cancellation = default)
        {
            var procedure = new UspGetPaymentsByUser(this) { UserId = userId };
            var queryResult = await procedure.QueryAsync<PaymentRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<List<PaymentSummaryResponse>>.Error(ErrorCode.DatabaseError);
            }

            return Result<List<PaymentSummaryResponse>>.Success(queryResult.Values1.Select(Map).ToList());
        }

        private async Task<Result<PaymentSummaryResponse?>> QuerySingleAsync(ProcedureBase procedure, CancellationToken cancellation)
        {
            var queryResult = await procedure.QueryAsync<PaymentRecord>(cancellation: cancellation);
            if (queryResult.IsError)
            {
                return Result<PaymentSummaryResponse?>.Error(ErrorCode.DatabaseError);
            }

            PaymentRecord? row = queryResult.Values1.FirstOrDefault();
            return Result<PaymentSummaryResponse?>.Success(row is null ? null : Map(row));
        }

        private static PaymentSummaryResponse Map(PaymentRecord row) => new()
        {
            OrderId = row.OrderId,
            OrderName = row.OrderName,
            Sport = EnumColumn.Read<Sport>(row.Sport),
            Amount = row.Amount,
            Currency = row.Currency,
            Status = EnumColumn.Read<PaymentStatus>(row.Status),
            PgProvider = EnumColumn.Read<PaymentProvider>(row.PgProvider),
            Method = row.Method,
            FailReason = row.FailReason,
            ApprovedAt = row.ApprovedAt,
            CreatedAt = row.CreatedAt
        };
    }
}
