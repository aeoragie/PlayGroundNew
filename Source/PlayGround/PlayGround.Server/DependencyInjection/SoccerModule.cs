using Microsoft.Extensions.DependencyInjection;
using PlayGround.Application.Landing.Commands;
using PlayGround.Application.Player.Commands;
using PlayGround.Application.Team.Commands;
using PlayGround.Persistence;

namespace PlayGround.Server.DependencyInjection
{
    /// <summary>축구 도메인: 저장소 + 유즈케이스(랜딩·선수·팀). 종목별로 이런 모듈을 하나씩 둔다.</summary>
    public static class SoccerModule
    {
        public static IServiceCollection AddSoccerServices(this IServiceCollection services)
        {
            services.AddSoccerPersistence();
            services.AddScoped<SoccerLandingContentsCommand>();
            services.AddScoped<SoccerPlayerProfileCommand>();
            services.AddScoped<SoccerTeamCommand>();
            services.AddScoped<SoccerTeamInfoCommand>();
            services.AddScoped<SoccerTeamRosterCommand>();
            return services;
        }
    }
}
