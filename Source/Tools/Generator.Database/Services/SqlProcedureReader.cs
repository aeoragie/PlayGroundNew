using Generator.Database.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Generator.Database.Services
{
    public class SqlProcedureReader
    {
        private readonly string ProceduresPath;

        public SqlProcedureReader(string proceduresPath)
        {
            ProceduresPath = proceduresPath;
        }

        public List<ProcedureSchema> ReadProceduresFromSqlFiles()
        {
            var procedures = new List<ProcedureSchema>();

            if (!Directory.Exists(ProceduresPath))
            {
                Console.WriteLine($"Warning: Procedures directory not found: {ProceduresPath}");
                return procedures;
            }

            var sqlFiles = Directory.GetFiles(ProceduresPath, "*.sql", SearchOption.AllDirectories);
            foreach (var sqlFile in sqlFiles)
            {
                try
                {
                    var procedure = ParseSqlFile(sqlFile);
                    if (procedure != null)
                    {
                        procedures.Add(procedure);
                        Console.WriteLine($"Parsed procedure: {Path.GetFileName(sqlFile)} -> [{procedure.Schema}].[{procedure.ProcedureName}] ({procedure.Parameters.Count} params)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing {sqlFile}: {ex.Message}");
                    Debug.Assert(false, ex.Message);
                }
            }

            return procedures;
        }

        private ProcedureSchema? ParseSqlFile(string filePath)
        {
            var content = File.ReadAllText(filePath);

            // CREATE PROCEDURE [schema].[name] 패턴 매칭
            var procPattern = @"CREATE\s+PROCEDURE\s+\[?(\w+)\]?\.\[?(\w+)\]?";
            var procMatch = Regex.Match(content, procPattern, RegexOptions.IgnoreCase);

            if (!procMatch.Success)
            {
                return null;
            }

            var schema = procMatch.Groups[1].Value;
            var procedureName = procMatch.Groups[2].Value;

            // dbo가 아닌 스키마는 생성 제외
            if (!string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Skipped (nested schema [{schema}]): {Path.GetFileName(filePath)}");
                return null;
            }

            var parameters = ParseParameters(content, procMatch.Index + procMatch.Length);
            var entityName = ParseAnnotationValue(content, "@entity");
            var sourceType = ParseAnnotationValue(content, "@source");
            var joinSources = ParseJoinAnnotations(content);

            return new ProcedureSchema
            {
                Schema = schema,
                ProcedureName = procedureName,
                Parameters = parameters,
                EntityName = entityName,
                SourceType = sourceType,
                JoinSources = joinSources
            };
        }

        private static string? ParseAnnotationValue(string content, string key)
        {
            var pattern = $@"--\s*{Regex.Escape(key)}:\s*(.+)";
            var match = Regex.Match(content, pattern, RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static List<JoinSource> ParseJoinAnnotations(string content)
        {
            // -- @join: TableName AS alias (Col1, Col2, ...)
            var pattern = @"--\s*@join:\s*(\w+)\s+AS\s+(\w+)\s*\(([^)]+)\)";
            var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase);

            var sources = new List<JoinSource>();
            foreach (Match m in matches)
            {
                var columns = m.Groups[3].Value
                    .Split(',')
                    .Select(c => c.Trim())
                    .Where(c => !string.IsNullOrEmpty(c))
                    .ToList();

                sources.Add(new JoinSource
                {
                    TableName = m.Groups[1].Value,
                    Alias = m.Groups[2].Value,
                    Columns = columns
                });
            }

            return sources;
        }

        private List<ParameterSchema> ParseParameters(string content, int startIndex)
        {
            var parameters = new List<ParameterSchema>();

            // CREATE PROCEDURE 이후 영역에서 AS 키워드 탐색 (주석 내 AS 오탐 방지)
            var asPattern = @"\bAS\b";
            var asRegex = new Regex(asPattern, RegexOptions.IgnoreCase);
            var asMatch = asRegex.Match(content, startIndex);

            if (!asMatch.Success)
            {
                return parameters;
            }

            var paramSection = content.Substring(startIndex, asMatch.Index - startIndex);
            var lines = paramSection.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (!trimmed.StartsWith("@"))
                {
                    continue;
                }

                var param = ParseParameterDefinition(trimmed);
                if (param != null)
                {
                    parameters.Add(param);
                }
            }

            return parameters;
        }

        private ParameterSchema? ParseParameterDefinition(string line)
        {
            // @ParamName TYPE(length) [= DEFAULT][,]
            var paramPattern = @"^@(\w+)\s+(\w+)(?:\(([^\)]+)\))?\s*(?:=\s*(.+?))?\s*,?\s*$";
            var match = Regex.Match(line, paramPattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            var paramName = match.Groups[1].Value;
            var dataType = match.Groups[2].Value.ToUpper();
            var lengthPart = match.Groups[3].Value;
            var defaultPart = match.Groups[4].Value.Trim();

            var hasDefault = !string.IsNullOrEmpty(defaultPart);
            var isNullable = hasDefault && defaultPart.Equals("NULL", StringComparison.OrdinalIgnoreCase);

            // 길이 파싱
            int? maxLength = null;
            int? precision = null;
            int? scale = null;

            if (!string.IsNullOrEmpty(lengthPart))
            {
                if (lengthPart.Equals("MAX", StringComparison.OrdinalIgnoreCase))
                {
                    maxLength = -1;
                }
                else if (lengthPart.Contains(','))
                {
                    var parts = lengthPart.Split(',');
                    if (int.TryParse(parts[0].Trim(), out var p))
                    {
                        precision = p;
                    }

                    if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var s))
                    {
                        scale = s;
                    }
                }
                else if (int.TryParse(lengthPart, out var len))
                {
                    maxLength = len;
                }
            }

            return new ParameterSchema
            {
                ParameterName = paramName,
                DataType = dataType,
                HasDefault = hasDefault,
                DefaultValue = hasDefault ? defaultPart : null,
                IsNullable = isNullable,
                CharacterMaximumLength = maxLength,
                NumericPrecision = precision,
                NumericScale = scale
            };
        }
    }
}
