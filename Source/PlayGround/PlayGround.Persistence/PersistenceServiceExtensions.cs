using Microsoft.Extensions.DependencyInjection;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Repositories;

namespace PlayGround.Persistence
{
    /// <summary>Persistence 계층 DI 등록. Server 조립 루트에서 호출.</summary>
    public static class PersistenceServiceExtensions
    {
        /// <summary>공용 신원(Account) 저장소 — 종목 무관.</summary>
        public static IServiceCollection AddAccountPersistence(this IServiceCollection services)
        {
            services.AddScoped<IAccountRepository, AccountRepository>();
            return services;
        }

        /// <summary>축구 도메인 저장소.</summary>
        public static IServiceCollection AddSoccerPersistence(this IServiceCollection services)
        {
            services.AddScoped<ILandingContentRepository, SoccerLandingContentRepository>();
            services.AddScoped<IPlayerRepository, SoccerPlayerRepository>();
            services.AddScoped<ISoccerTeamRepository, SoccerTeamRepository>();
            services.AddScoped<ISoccerRecordsRepository, SoccerRecordsRepository>();
            return services;
        }
    }
}
