using System.Diagnostics;
using Microsoft.Extensions.Hosting;
using PlayGround.Infrastructure.Actor;
using PlayGround.Infrastructure.Logging;

namespace PlayGround.Server.Actors
{
    /// <summary>애플리케이션 액터 토폴로지 구성. AkkaService(ActorSystem 기동) 다음에 등록되어,
    /// StartAsync 시점엔 ActorSystem이 준비돼 있다 (IHostedService는 등록 순서대로 기동).</summary>
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
            // 읽기 부하 분산용 RoundRobin 풀. ReceiveActorBase(IServiceProvider) 생성자 인자는
            // DI 리졸버가 채우므로 args는 비운다.
            mAkka.CreateRouter<LandingActor>(ActorNames.Landing, poolSize: 4);
            Logger.InfoWith("Actor topology created", ("Landing", ActorNames.Landing));
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
