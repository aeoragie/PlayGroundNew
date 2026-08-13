using PlayGround.Application.Interfaces;
using PlayGround.Application.Payment.Commands;
using PlayGround.Infrastructure.Logging;
using PlayGround.Persistence;
using PlayGround.Server.Services;

namespace PlayGround.Server.DependencyInjection
{
    /// <summary>결제 (종목 공통): 원장 저장소 + PG 어댑터 + 유즈케이스. Provider가 어댑터를 고른다.</summary>
    public static class PaymentModule
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        public static IServiceCollection AddPaymentServices(this IServiceCollection services, IConfiguration configuration)
        {
            PaymentConfiguration paymentConfig =
                configuration.GetSection(PaymentConfiguration.Section).Get<PaymentConfiguration>()
                ?? new PaymentConfiguration();

            // 키 값 자체는 절대 로깅하지 않는다 — 켜졌는지와 벤더만 남긴다.
            KeyValueLogExtensions.Info(Logger, "Payment configured",
                ("Provider", paymentConfig.Provider),
                ("HasClientKey", !string.IsNullOrEmpty(paymentConfig.ClientKey)));

            if (paymentConfig.IsEnabled &&
                (string.IsNullOrWhiteSpace(paymentConfig.ClientKey) || string.IsNullOrWhiteSpace(paymentConfig.SecretKey)))
            {
                // 반쪽 설정으로 뜨면 결제창은 열리는데 승인만 죽는 상태가 된다 — 기동 실패가 낫다
                throw new InvalidOperationException(
                    $"PaymentConfiguration: Provider={paymentConfig.Provider} requires ClientKey and SecretKey");
            }

            services.AddSingleton(paymentConfig);
            services.AddSingleton<IPaymentGateway>(sp => CreateGateway(paymentConfig, sp));

            services.AddPaymentPersistence();
            services.AddScoped<PaymentOrderCommand>();
            services.AddScoped<PaymentConfirmCommand>();
            services.AddScoped<PaymentFailCommand>();
            services.AddScoped<PaymentHistoryCommand>();

            return services;
        }

        // Provider 값 하나가 어댑터 하나에 대응한다 — 새 PG를 붙이면 여기에 한 줄이 는다.
        private static IPaymentGateway CreateGateway(PaymentConfiguration config, IServiceProvider provider) => config.Provider switch
        {
            PaymentProviderKind.Toss => new TossPaymentGatewayService(
                provider.GetRequiredService<IHttpClientFactory>(), config),
            _ => new DisabledPaymentGatewayService(),
        };
    }
}
