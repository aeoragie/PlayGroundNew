using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Config;
using NLog.Extensions.Hosting;
using NLog.Targets;
using PlayGround.Infrastructure.Logging.Render;

namespace PlayGround.Infrastructure.Logging
{
    /// <summary>
    /// NLog 설정 및 DI 등록 확장 메서드
    /// </summary>
    public static class LoggingExtensions
    {
        /// <summary>
        /// IHostBuilder에 NLog 로깅 구성
        /// </summary>
        public static IHostBuilder ConfigurePlayGroundLogger(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            hostBuilder.ConfigureLogging((context, logging) =>
            {
                logging.ClearProviders();
                RegisterCustomRenderers();

                var configPath = GetLogConfigPath();
                ConfigureNLog(configPath, configuration);
            }).UseNLog();

            return hostBuilder;
        }

        /// <summary>
        /// NLog ILogger를 DI 컨테이너에 등록
        /// </summary>
        public static IServiceCollection AddPlayGroundLogger(this IServiceCollection services)
        {
            services.AddSingleton<NLog.ILogger>(LogManager.GetCurrentClassLogger());
            return services;
        }

        #region Private

        private static void RegisterCustomRenderers()
        {
            LogManager.Setup().SetupExtensions(ext =>
            {
                ext.RegisterLayoutRenderer<ArchiveDateLayoutRenderer>("archivedate");
                ext.RegisterLayoutRenderer<PaddedThreadIdLayoutRenderer>("paddedthreadid");
            });
        }

        /// <summary>
        /// 환경별 NLog 설정 파일 경로 결정
        /// nlog.{environment}.config 우선, 없으면 nlog.config
        /// </summary>
        private static string GetLogConfigPath()
        {
            var baseDirectory = AppContext.BaseDirectory;

            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
                ?? "Production";

            var envConfigPath = Path.Combine(baseDirectory, $"nlog.{environment}.config");
            if (File.Exists(envConfigPath))
            {
                return envConfigPath;
            }

            return Path.Combine(baseDirectory, "nlog.config");
        }

        private static void ConfigureNLog(string configPath, IConfiguration configuration)
        {
            if (File.Exists(configPath))
            {
                LogManager.Configuration = new XmlLoggingConfiguration(configPath);
            }

            ApplySettings(configuration);
            LogManager.ReconfigExistingLoggers();
        }

        /// <summary>
        /// appsettings.json의 LoggingConfig 섹션으로 NLog 런타임 설정 적용
        /// </summary>
        private static void ApplySettings(IConfiguration configuration)
        {
            var options = configuration.GetSection(LoggingConfig.Section).Get<LoggingConfig>();
            if (options == null)
            {
                return;
            }

            var config = LogManager.Configuration;
            if (config == null)
            {
                return;
            }

            // 파일 로깅: 아카이브 설정
            if (options.EnableFileLogging)
            {
                var fileTarget = config.FindTargetByName<FileTarget>("FileLogger");
                if (fileTarget != null && options.MaxArchiveFiles > 0)
                {
                    fileTarget.ArchiveOldFileOnStartup = true;
                    fileTarget.MaxArchiveFiles = options.MaxArchiveFiles;
                }
            }

            // 콘솔 로깅 비활성화
            if (!options.EnableConsoleLogging)
            {
                var consoleTarget = config.FindTargetByName("ConsoleLogger");
                if (consoleTarget != null)
                {
                    var rulesToUpdate = config.LoggingRules
                        .Where(rule => rule.Targets.Contains(consoleTarget))
                        .ToList();

                    foreach (var rule in rulesToUpdate)
                    {
                        rule.Targets.Remove(consoleTarget);
                    }
                }
            }

            // 로그 레벨 동적 변경
            if (!string.IsNullOrEmpty(options.LogLevel))
            {
                var logLevel = NLog.LogLevel.FromString(options.LogLevel);
                foreach (var rule in config.LoggingRules)
                {
                    if (rule.LoggerNamePattern == "*")
                    {
                        rule.SetLoggingLevels(logLevel, NLog.LogLevel.Fatal);
                    }
                }
            }
        }

        #endregion
    }
}
