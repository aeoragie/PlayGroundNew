namespace Generator.Database.Configuration
{
    public class DatabaseOptions
    {
        public string SqlTablesPath { get; set; } = string.Empty;
        public string SqlProceduresPath { get; set; } = string.Empty;
        public string SqlQueriesPath { get; set; } = string.Empty;
        public PathOptions Paths { get; set; } = new();
    }
}
