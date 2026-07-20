using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using PlayGround.Infrastructure.Actor;
using PlayGround.Infrastructure.Logging;

namespace PlayGround.Server.Actors
{
    /// <summary>애플리케이션 액터 토폴로지 구성. AkkaService 기동 후 실행된다.</summary>
    public sealed class ActorTopologyService : IHostedService
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly AkkaService mAkka;

        public ActorTopologyService(AkkaService akka)
        {
            ArgumentNullException.ThrowIfNull(akka);
            Debug.Assert(akka is not null);
            mAkka = akka;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            mAkka.CreateRouter<SoccerLandingActor>(ActorNames.SoccerLanding, poolSize: 4);                  // 읽기: RoundRobin 풀
            mAkka.CreateRouter<SoccerTeamInfoActor>(ActorNames.SoccerTeamInfo, poolSize: 4);                // 읽기: RoundRobin 풀
            mAkka.CreateRouter<SoccerRecordsActor>(ActorNames.SoccerRecords, poolSize: 4);                  // 읽기: RoundRobin 풀 (공개)
            mAkka.CreateRouter<SoccerDashboardActor>(ActorNames.SoccerDashboard, poolSize: 4);              // 읽기: RoundRobin 풀 (허브)
            mAkka.CreateHashRouter<SoccerPlayerProfileActor>(ActorNames.SoccerPlayerProfile, poolSize: 4);  // 쓰기: UserId 해시(사용자별 순차)
            mAkka.CreateHashRouter<SoccerTeamProfileActor>(ActorNames.SoccerTeamProfile, poolSize: 4);      // 쓰기: ManagerUserId 해시
            Logger.InfoWith("Actor topology created",
                ("Landing", ActorNames.SoccerLanding), ("TeamInfo", ActorNames.SoccerTeamInfo),
                ("PlayerProfile", ActorNames.SoccerPlayerProfile), ("TeamProfile", ActorNames.SoccerTeamProfile));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
