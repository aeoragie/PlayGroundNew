using Akka.Actor;
using Akka.Configuration;
using Akka.DependencyInjection;
using Akka.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using NLog;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Primitives;
using System.Collections.Concurrent;

namespace PlayGround.Infrastructure.Actor
{
    public class AkkaConfig
    {
        public static readonly string Section = "AkkaConfig";

        public string SystemName { get; set; } = "PlayGroundActorSystem";
        public string? ConfFileName { get; set; }
    }

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

            Logging.KeyValueLogExtensions.Info(Logger, "ActorSystem started", ("SystemName", akkaConfig.SystemName));
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (ActorSystem != null)
            {
                await CoordinatedShutdown.Get(ActorSystem)
                    .Run(CoordinatedShutdown.ClrExitReason.Instance);
                Logging.KeyValueLogExtensions.Info(Logger, "ActorSystem stopped");
            }
        }

        #region Actor Creation

        public ActorRef? CreateActor<TActor>(string actorName, params object[] args)
            where TActor : ActorBase
        {
            return CreateActorCore<TActor>(actorName, props => props, args);
        }

        public ActorRef? CreateRouter<TActor>(string routerName, int poolSize, params object[] args)
            where TActor : ActorBase
        {
            return CreateActorCore<TActor>(routerName, props => props.WithRouter(new RoundRobinPool(poolSize)), args);
        }

        public ActorRef? CreateHashRouter<TActor>(string routerName, int poolSize, params object[] args)
            where TActor : ActorBase
        {
            return CreateActorCore<TActor>(routerName, props => props.WithRouter(new ConsistentHashingPool(poolSize)), args);
        }

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
            if (ActorSystem is null)
            {
                Panic.Fail("ActorSystem is not started.");
            }

            // 중복 이름을 먼저 걸러 고아 액터 생성을 방지
            if (Actors.ContainsKey(actorName))
            {
                Logging.KeyValueLogExtensions.Warn(Logger, "Actor already exists", ("ActorName", actorName));
                return null;
            }

            try
            {
                var props = configureProps(DependencyResolver.For(ActorSystem).Props<TActor>(args));
                var actorRef = ActorSystem.ActorOf(props, actorName);

                var actor = new ActorRef(actorRef, actorName);
                if (!Actors.TryAdd(actorName, actor))
                {
                    Logging.KeyValueLogExtensions.Warn(Logger, "Actor registration raced, stopping orphan", ("ActorName", actorName));
                    ActorSystem.Stop(actorRef);
                    return null;
                }

#pragma warning disable CS0162
                DiagnosticLog.Actor(Logger, "Actor created", ("ActorName", actorName));
#pragma warning restore CS0162
                return actor;
            }
            catch (InvalidActorNameException ex)
            {
                Logging.KeyValueLogExtensions.Error(Logger, ex, "Actor creation failed", ("ActorName", actorName));
                return null;
            }
        }

        #endregion
    }
}
