using Generator.Database.Configuration;
using Generator.Database.Generators;
using Generator.Database.Models;
using System.Text;

namespace Generator.Database.Services
{
    public class CodeGeneratorService
    {
        private readonly string mCommonPath;
        private readonly PathOptions mPaths;
        private readonly string mRootNamespace;

        public CodeGeneratorService(string commonPath, PathOptions path, string rootNamespace)
        {
            mCommonPath = commonPath;
            mPaths = path;
            mRootNamespace = rootNamespace;
        }

        public async Task<(List<string>, List<string>)> GenerateCodesAsync(string database, DatabaseSchema schema)
        {
            var generatedFiles = new List<string>();
            var allFiles = new List<string>();

            Console.WriteLine($"Generating codes for database: {schema.DatabaseName}");

            var tableCode = await GenerateEntitiesAsync(database, schema.Tables);
            generatedFiles.AddRange(tableCode.Item1);
            allFiles.AddRange(tableCode.Item2);

            var joinEntityCode = await GenerateJoinEntitiesAsync(database, schema.Procedures, schema.Tables);
            generatedFiles.AddRange(joinEntityCode.Item1);
            allFiles.AddRange(joinEntityCode.Item2);

            var procedureCode = await GenerateProceduresAsync(database, schema.Procedures);
            generatedFiles.AddRange(procedureCode.Item1);
            allFiles.AddRange(procedureCode.Item2);

            var queryCode = await GenerateQueriesAsync(database, schema.Queries);
            generatedFiles.AddRange(queryCode.Item1);
            allFiles.AddRange(queryCode.Item2);

            Console.WriteLine($"Generated {generatedFiles.Count} files successfully.");

            return (generatedFiles, allFiles);
        }

        private async Task<(List<string>, List<string>)> GenerateEntitiesAsync(string database, List<TableSchema> tables)
        {
            var directoryPath = Path.Combine(mCommonPath, mPaths.TablePath);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }

            var generatedFiles = new List<string>();
            var allFiles = new List<string>();

            Console.WriteLine("📋 Generating table entities...");

            var generator = new TableEntityGenerator(mRootNamespace);
            var generatedCodes = generator.GenerateEntities(database, tables);

            foreach (var generatedCode in generatedCodes)
            {
                var saveResult = await SaveCodeToFileAsync(mPaths.TablePath, generatedCode);
                if (saveResult.Generated && !string.IsNullOrEmpty(saveResult.Path))
                {
                    generatedFiles.Add(saveResult.Path);
                    Console.WriteLine($"Generated entity: {generatedCode.FileName}");
                }

                allFiles.Add(saveResult.Path);
            }

            return (generatedFiles, allFiles);
        }

        private async Task<(List<string>, List<string>)> GenerateJoinEntitiesAsync(string database, List<ProcedureSchema> procedures, List<TableSchema> tables)
        {
            var generatedFiles = new List<string>();
            var allFiles = new List<string>();

            var joinProcedures = procedures
                .Where(p => !string.IsNullOrEmpty(p.EntityName) &&
                            string.Equals(p.SourceType, "join", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (joinProcedures.Count == 0)
            {
                return (generatedFiles, allFiles);
            }

            Console.WriteLine("📋 Generating join entities...");

            var generator = new JoinEntityGenerator(mRootNamespace);
            var generatedCodes = generator.GenerateJoinEntities(database, joinProcedures, tables);

            foreach (var generatedCode in generatedCodes)
            {
                var saveResult = await SaveCodeToFileAsync(mPaths.TablePath, generatedCode);
                if (saveResult.Generated && !string.IsNullOrEmpty(saveResult.Path))
                {
                    generatedFiles.Add(saveResult.Path);
                    Console.WriteLine($"Generated join entity: {generatedCode.FileName}");
                }

                allFiles.Add(saveResult.Path);
            }

            return (generatedFiles, allFiles);
        }

        private async Task<(List<string>, List<string>)> GenerateProceduresAsync(string database, List<ProcedureSchema> procedures)
        {
            if (string.IsNullOrEmpty(mPaths.ProcedurePath))
            {
                return (new List<string>(), new List<string>());
            }

            var directoryPath = Path.Combine(mCommonPath, mPaths.ProcedurePath);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }

            var generatedFiles = new List<string>();
            var allFiles = new List<string>();

            Console.WriteLine("📋 Generating procedure wrappers...");

            var generator = new ProcedureGenerator(mRootNamespace);
            var generatedCodes = generator.GenerateProcedures(database, procedures);

            foreach (var generatedCode in generatedCodes)
            {
                var saveResult = await SaveCodeToFileAsync(mPaths.ProcedurePath, generatedCode);
                if (saveResult.Generated && !string.IsNullOrEmpty(saveResult.Path))
                {
                    generatedFiles.Add(saveResult.Path);
                    Console.WriteLine($"Generated procedure: {generatedCode.FileName}");
                }

                allFiles.Add(saveResult.Path);
            }

            return (generatedFiles, allFiles);
        }

        private async Task<(List<string>, List<string>)> GenerateQueriesAsync(string database, List<QuerySchema> queries)
        {
            if (string.IsNullOrEmpty(mPaths.QueryPath))
            {
                return (new List<string>(), new List<string>());
            }

            var directoryPath = Path.Combine(mCommonPath, mPaths.QueryPath);
            if (Directory.Exists(directoryPath))
            {
                Directory.Delete(directoryPath, true);
            }

            var generatedFiles = new List<string>();
            var allFiles = new List<string>();

            Console.WriteLine("📋 Generating query wrappers...");

            var generator = new QueryGenerator(mRootNamespace);
            var generatedCodes = generator.GenerateQueries(database, queries);

            foreach (var generatedCode in generatedCodes)
            {
                var saveResult = await SaveCodeToFileAsync(mPaths.QueryPath, generatedCode);
                if (saveResult.Generated && !string.IsNullOrEmpty(saveResult.Path))
                {
                    generatedFiles.Add(saveResult.Path);
                    Console.WriteLine($"Generated query: {generatedCode.FileName}");
                }

                allFiles.Add(saveResult.Path);
            }

            return (generatedFiles, allFiles);
        }

        private async Task<(bool Generated, string Path)> SaveCodeToFileAsync(string outputPath, GeneratedFile generated)
        {
            var directoryPath = Path.Combine(mCommonPath, outputPath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var filePath = Path.Combine(directoryPath, generated.FileName);

            try
            {
                if (File.Exists(filePath))
                {
                    var existingContent = await File.ReadAllTextAsync(filePath);
                    if (NormalizeContent(existingContent) == NormalizeContent(generated.Content))
                    {
                        Console.WriteLine($"Skipped (no changes): {generated.FileName}");
                        return (false, filePath);
                    }
                }

                var normalizedContent = NormalizeToCrlf(generated.Content);
                await File.WriteAllTextAsync(filePath, normalizedContent, Encoding.UTF8);

                return (true, filePath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving file {generated.FileName}: {ex.Message}");
                return (false, filePath);
            }
        }

        private static string NormalizeContent(string content)
        {
            return content?.Replace("\r\n", "\n").Replace("\r", "\n").Trim() ?? string.Empty;
        }

        private static string NormalizeToCrlf(string content)
        {
            return content?.Replace("\r\n", "\n").Replace("\r", "\n").Replace("\n", "\r\n") ?? string.Empty;
        }

        public string GenerateSummary(List<string> generatedFiles)
        {
            if (generatedFiles.Count == 0)
            {
                return "No files were generated.";
            }

            var summary = new StringBuilder();
            summary.AppendLine($"Successfully generated {generatedFiles.Count} files:");
            summary.AppendLine();

            var groupedFiles = generatedFiles
                .GroupBy(f => Path.GetDirectoryName(f))
                .OrderBy(g => g.Key);

            foreach (var group in groupedFiles)
            {
                var relativePath = Path.GetRelativePath(mCommonPath, group.Key ?? string.Empty);
                summary.AppendLine($"📁 {relativePath}");

                foreach (var file in group.OrderBy(f => f))
                {
                    var fileName = Path.GetFileName(file);
                    summary.AppendLine($"  📄 {fileName}");
                }
                summary.AppendLine();
            }

            return summary.ToString();
        }
    }
}
