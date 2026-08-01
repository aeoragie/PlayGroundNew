using Generator.Database.Configuration;
using Generator.Database.Services;
using Microsoft.Extensions.Configuration;
using System.Text;

Console.Title = "Database Code Generator";
Console.OutputEncoding = Encoding.UTF8;

try
{
    Console.WriteLine("🚀 Database Code Generator");
    Console.WriteLine("==========================");
    Console.WriteLine();

    var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
        .Build();

    Console.WriteLine($"📋 Configuration: appsettings.json");
    var dbConfig = configuration.GetSection("CodeGenerationSettings").Get<CodeGenerationSettings>();
    if (dbConfig == null)
    {
        ShowUsage();
        return 1;
    }

    foreach (var database in dbConfig.Databases)
    {
        var value = database.Value;
        Console.WriteLine($"📦 Database: {database.Key}");
        Console.WriteLine($"   Output Entities: {value.Paths.TablePath}");
        Console.WriteLine($"   Output Procedures: {value.Paths.ProcedurePath}");
        Console.WriteLine($"   Output Queries: {value.Paths.QueryPath}");
        Console.WriteLine($"   SQL Tables Path: {value.SqlTablesPath}");
        Console.WriteLine($"   SQL Procedures Path: {value.SqlProceduresPath}");
        Console.WriteLine($"   SQL Queries Path: {value.SqlQueriesPath}");
        Console.WriteLine();

        Console.WriteLine($"📖 Reading schema from SQL files...");

        var tables = new List<Generator.Database.Models.TableSchema>();
        if (!string.IsNullOrEmpty(value.SqlTablesPath))
        {
            var sqlFileReader = new SqlFileSchemaReader(value.SqlTablesPath);
            tables = sqlFileReader.ReadTablesFromSqlFiles();
        }

        var procedures = new List<Generator.Database.Models.ProcedureSchema>();
        if (!string.IsNullOrEmpty(value.SqlProceduresPath))
        {
            var sqlProcReader = new SqlProcedureReader(value.SqlProceduresPath);
            procedures = sqlProcReader.ReadProceduresFromSqlFiles();
        }

        var queries = new List<Generator.Database.Models.QuerySchema>();
        if (!string.IsNullOrEmpty(value.SqlQueriesPath))
        {
            var sqlQueryReader = new SqlQueryReader(value.SqlQueriesPath);
            queries = sqlQueryReader.ReadQueriesFromSqlFiles();
        }

        var schema = new Generator.Database.Models.DatabaseSchema
        {
            DatabaseName = database.Key,
            Tables = tables,
            Procedures = procedures,
            Queries = queries
        };

        Console.WriteLine($"✅ Found {schema.Tables.Count} tables, {schema.Procedures.Count} procedures, {schema.Queries.Count} queries from SQL files");
        Console.WriteLine();

        Console.WriteLine("⚙️ Generating C# code...");
        var codeGenerator = new CodeGeneratorService(dbConfig.CommonPath, value.Paths, dbConfig.RootNamespace);
        var generatedFiles = await codeGenerator.GenerateCodesAsync(database.Key, schema);

        Console.WriteLine();
        Console.WriteLine("📊 Generation Summary:");
        Console.WriteLine("=====================");
        Console.WriteLine(codeGenerator.GenerateSummary(generatedFiles.Item1));
    }

    Console.WriteLine("🎉 Code generation completed successfully!");

    return 0;
}
catch (Exception ex)
{
    Console.WriteLine();
    Console.WriteLine("❌ Error occurred during code generation:");
    Console.WriteLine($"   {ex.Message}");

    if (args.Contains("--verbose"))
    {
        Console.WriteLine();
        Console.WriteLine("Stack Trace:");
        Console.WriteLine(ex.StackTrace);
    }

    return 1;
}

static void ShowUsage()
{
    Console.WriteLine("Usage: Generator.Database [options]");
    Console.WriteLine();
    Console.WriteLine("Options:");
    Console.WriteLine("  --verbose    Show detailed error information");
    Console.WriteLine();
    Console.WriteLine("Configuration is read from appsettings.json");
}
