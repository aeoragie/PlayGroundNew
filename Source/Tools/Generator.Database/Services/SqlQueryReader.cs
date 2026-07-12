using Generator.Database.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Generator.Database.Services
{
    public class SqlQueryReader
    {
        private readonly string QueriesPath;

        public SqlQueryReader(string queriesPath)
        {
            QueriesPath = queriesPath;
        }

        public List<QuerySchema> ReadQueriesFromSqlFiles()
        {
            var queries = new List<QuerySchema>();

            if (!Directory.Exists(QueriesPath))
            {
                Console.WriteLine($"Warning: Queries directory not found: {QueriesPath}");
                return queries;
            }

            var sqlFiles = Directory.GetFiles(QueriesPath, "*.sql", SearchOption.AllDirectories);
            foreach (var sqlFile in sqlFiles)
            {
                try
                {
                    var query = ParseSqlFile(sqlFile);
                    if (query != null)
                    {
                        queries.Add(query);
                        Console.WriteLine($"Parsed query: {Path.GetFileName(sqlFile)} -> {query.QueryName} ({query.Parameters.Count} params)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing {sqlFile}: {ex.Message}");
                    Debug.Assert(false, ex.Message);
                }
            }

            return queries;
        }

        private QuerySchema? ParseSqlFile(string filePath)
        {
            var queryName = Path.GetFileNameWithoutExtension(filePath);
            var lines = File.ReadAllLines(filePath);

            var parameters = new List<QueryParameterSchema>();
            var sqlStartIndex = 0;

            // 파일 상단의 -- @param: 주석에서 파라미터 파싱
            for (var i = 0; i < lines.Length; i++)
            {
                var trimmed = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // -- @param: ParamName TYPE [NULL]
                var paramMatch = Regex.Match(trimmed, @"^--\s*@param:\s*(\w+)\s+(\w+(?:\(\S+?\))?)\s*(NULL)?", RegexOptions.IgnoreCase);
                if (paramMatch.Success)
                {
                    parameters.Add(new QueryParameterSchema
                    {
                        ParameterName = paramMatch.Groups[1].Value,
                        DataType = paramMatch.Groups[2].Value,
                        IsNullable = paramMatch.Groups[3].Success
                    });
                    sqlStartIndex = i + 1;
                    continue;
                }

                // @param이 아닌 주석은 스킵 (description 등)
                if (trimmed.StartsWith("--"))
                {
                    continue;
                }

                // SQL 본문 시작
                sqlStartIndex = i;
                break;
            }

            var sqlBody = string.Join(Environment.NewLine, lines.Skip(sqlStartIndex)).Trim();

            if (string.IsNullOrWhiteSpace(sqlBody))
            {
                return null;
            }

            return new QuerySchema
            {
                QueryName = queryName,
                SqlBody = sqlBody,
                Parameters = parameters
            };
        }
    }
}
