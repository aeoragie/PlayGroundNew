using Microsoft.Extensions.DependencyInjection;
using PlayGround.Application.Interfaces;
using PlayGround.Persistence.Repositories;

namespace PlayGround.Persistence
{
    /// <summary>Persistence 계층 DI 등록. Server 조립 루트에서 호출.</summary>
    public static class PersistenceServiceExtensions
    {
        public static IServiceCollection AddPersistence(this IServiceCollection services)
        {
            services.AddScoped<ILandingContentRepository, LandingContentRepository>();
            services.AddScoped<IAccountRepository, AccountRepository>();
            return services;
        }
    }
}
