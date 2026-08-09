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
    public static class LoggingExtensions
    {
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

            if (options.EnableFileLogging)
            {
                var fileTarget = config.FindTargetByName<FileTarget>("FileLogger");
                if (fileTarget != null && options.MaxArchiveFiles > 0)
                {
                    fileTarget.ArchiveOldFileOnStartup = true;
                    fileTarget.MaxArchiveFiles = options.MaxArchiveFiles;
                }
            }

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
