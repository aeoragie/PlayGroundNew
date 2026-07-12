using Microsoft.Extensions.DependencyInjection;
using PlayGround.Infrastructure.Actor;
using PlayGround.Server.Actors;

namespace PlayGround.Server.DependencyInjection
{
    /// <summary>Akka 처리 파이프라인(Controller → 액터 → 유즈케이스) 인프라 등록.</summary>
    public static class AkkaModule
    {
        public static IServiceCollection AddAkkaPipeline(this IServiceCollection services)
        {
            // AkkaService(HostedService) 기동 후 토폴로지가 액터를 만든다 — 등록 순서 유지
            services.AddSingleton<AkkaService>();
            services.AddHostedService(sp => sp.GetRequiredService<AkkaService>());
            services.AddSingleton<ActorGateway>();
            services.AddHostedService<ActorTopologyService>();
            return services;
        }
    }
}
