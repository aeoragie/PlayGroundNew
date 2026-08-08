using Microsoft.Extensions.DependencyInjection;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Repositories;

namespace PlayGround.Persistence
{
    /// <summary>Persistence 계층 DI 등록. Server 조립 루트에서 호출.</summary>
    public static class PersistenceServiceExtensions
    {
        public static IServiceCollection AddAccountPersistence(this IServiceCollection services)
        {
            services.AddScoped<IAccountRepository, AccountRepository>();
            return services;
        }

        public static IServiceCollection AddSoccerPersistence(this IServiceCollection services)
        {
            services.AddScoped<ILandingContentRepository, SoccerLandingContentRepository>();
            services.AddScoped<IPlayerRepository, SoccerPlayerRepository>();
            services.AddScoped<ISoccerTeamRepository, SoccerTeamRepository>();
            services.AddScoped<ISoccerRecordsRepository, SoccerRecordsRepository>();
            services.AddScoped<IClaimRepository, SoccerClaimRepository>();
            services.AddScoped<INotificationRepository, SoccerNotificationRepository>();
            services.AddScoped<IOgMetaRepository, OgMetaRepository>();
            services.AddScoped<IAgentApprovalRepository, SoccerAgentApprovalRepository>();
            services.AddScoped<ISoccerDataExportRepository, SoccerDataExportRepository>();
            return services;
        }
    }
}
