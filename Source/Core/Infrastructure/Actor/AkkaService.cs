using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Akka.Actor;
using Akka.Configuration;
using Akka.DependencyInjection;
using Akka.Routing;
using NLog;

namespace PlayGround.Infrastructure.Actor
{
    /// <summary>
    /// Akka 설정 (appsettings.json)
    /// </summary>
    public class AkkaConfig
    {
        public static readonly string Section = "AkkaConfig";

        public string SystemName { get; set; } = "PlayGroundActorSystem";
        public string? ConfFileName { get; set; }
    }

    /// <summary>
    /// Akka ActorSystem 생명주기 관리 및 액터 생성.
    /// 액터 생성은 DI 리졸버를 경유하므로 액터 생성자에 등록된 서비스가 주입된다.
    /// </summary>
    public class AkkaService : IHostedService
    {
        private static readonly ILogger Logger = LogManager.GetCurrentClassLogger();

        private readonly IServiceProvider mServiceProvider;
        private readonly IConfiguration mConfiguration;
        private readonly IHostApplicationLifetime mApplicationLifetime;

        public ActorSystem? ActorSystem { get; private set; }
        public ConcurrentDictionary<string, ActorRef> Actors { get; } = new();

        public AkkaService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            IHostApplicationLifetime applicationLifetime)
        {
            mServiceProvider = serviceProvider;
            mConfiguration = configuration;
            mApplicationLifetime = applicationLifetime;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var akkaConfig = mConfiguration.GetSection(AkkaConfig.Section).Get<AkkaConfig>() ?? new AkkaConfig();

            var config = ConfigurationFactory.Default();
            if (!string.IsNullOrWhiteSpace(akkaConfig.ConfFileName) && File.Exists(akkaConfig.ConfFileName))
            {
                var hocon = await File.ReadAllTextAsync(akkaConfig.ConfFileName, cancellationToken);
                config = ConfigurationFactory.ParseString(hocon);
            }

            var bootstrap = BootstrapSetup.Create().WithConfig(config);
            var diSetup = DependencyResolverSetup.Create(mServiceProvider);
            var actorSystemSetup = bootstrap.And(diSetup);

            ActorSystem = ActorSystem.Create(akkaConfig.SystemName, actorSystemSetup);

            ActorSystem.WhenTerminated?.ContinueWith(_ =>
            {
                mApplicationLifetime.StopApplication();
            }, cancellationToken);

            Logger.Info("ActorSystem started. {{ SystemName:{SystemName} }}", akkaConfig.SystemName);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (ActorSystem != null)
            {
                await CoordinatedShutdown.Get(ActorSystem)
                    .Run(CoordinatedShutdown.ClrExitReason.Instance);
                Logger.Info("ActorSystem stopped.");
            }
        }

        #region Actor Creation

        /// <summary>
        /// 단일 액터 생성
        /// </summary>
        public ActorRef? CreateActor<TActor>(string actorName, params object[] args)
            where TActor : ActorBase
        {
            return CreateActorCore<TActor>(actorName, props => props, args);
        }

        /// <summary>
        /// RoundRobin 라우터 액터 생성
        /// </summary>
        public ActorRef? CreateRouter<TActor>(string routerName, int poolSize, params object[] args)
            where TActor : ActorBase
        {
            return CreateActorCore<TActor>(routerName, props => props.WithRouter(new RoundRobinPool(poolSize)), args);
        }

        /// <summary>
        /// ConsistentHash 라우터 액터 생성
        /// </summary>
        public ActorRef? CreateHashRouter<TActor>(string routerName, int poolSize, params object[] args)
            where TActor : ActorBase
        {
            return CreateActorCore<TActor>(routerName, props => props.WithRouter(new ConsistentHashingPool(poolSize)), args);
        }

        /// <summary>
        /// 이름으로 액터 조회
        /// </summary>
        public ActorRef? GetActor(string actorName)
        {
            if (Actors.TryGetValue(actorName, out var actor))
            {
                return actor;
            }

            return null;
        }

        private ActorRef? CreateActorCore<TActor>(string actorName, Func<Props, Props> configureProps, object[] args)
            where TActor : ActorBase
        {
            Debug.Assert(ActorSystem != null, "ActorSystem is not initialized");
            if (ActorSystem == null)
            {
                return null;
            }

            // 중복 이름을 먼저 걸러 고아 액터 생성을 방지
            if (Actors.ContainsKey(actorName))
            {
                Logger.Warn("Actor already exists. {{ ActorName:{ActorName} }}", actorName);
                return null;
            }

            try
            {
                var props = configureProps(DependencyResolver.For(ActorSystem).Props<TActor>(args));
                var actorRef = ActorSystem.ActorOf(props, actorName);

                var actor = new ActorRef(actorRef, actorName);
                if (!Actors.TryAdd(actorName, actor))
                {
                    Logger.Warn("Actor registration raced, stopping orphan. {{ ActorName:{ActorName} }}", actorName);
                    ActorSystem.Stop(actorRef);
                    return null;
                }

                Logger.Debug("Actor created. {{ ActorName:{ActorName} }}", actorName);
                return actor;
            }
            catch (InvalidActorNameException ex)
            {
                Logger.Error(ex, "Actor creation failed. {{ ActorName:{ActorName} }}", actorName);
                return null;
            }
        }

        #endregion
    }
}
