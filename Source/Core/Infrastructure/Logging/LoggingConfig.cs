namespace PlayGround.Infrastructure.Logging
{
    public class LoggingConfig
    {
        public static readonly string Section = "LoggingConfig";

        public string LogLevel { get; set; } = "Info";
        public bool EnableFileLogging { get; set; } = true;
        public bool EnableConsoleLogging { get; set; } = true;
        public int MaxArchiveFiles { get; set; } = 30;
    }
}
