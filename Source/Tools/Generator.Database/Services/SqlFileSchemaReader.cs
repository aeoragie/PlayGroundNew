using Generator.Database.Models;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace Generator.Database.Services
{
    public class SqlFileSchemaReader
    {
        private readonly string mTablesPath;

        public SqlFileSchemaReader(string tablesPath)
        {
            mTablesPath = tablesPath;
        }

        public List<TableSchema> ReadTablesFromSqlFiles()
        {
            var tables = new List<TableSchema>();

            if (!Directory.Exists(mTablesPath))
            {
                Console.WriteLine($"Warning: Tables directory not found: {mTablesPath}");
                return tables;
            }

            var sqlFiles = Directory.GetFiles(mTablesPath, "*.sql");
            foreach (var sqlFile in sqlFiles)
            {
                try
                {
                    var table = ParseSqlFile(sqlFile);
                    if (table != null)
                    {
                        tables.Add(table);
                        Console.WriteLine($"Parsed SQL file: {Path.GetFileName(sqlFile)} -> {table.TableName} ({table.Columns.Count} columns)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing {sqlFile}: {ex.Message}");
                    Debug.Assert(false, ex.Message);
                }
            }

            return tables;
        }

        private TableSchema? ParseSqlFile(string filePath)
        {
            var content = File.ReadAllText(filePath);

            var tablePattern = @"CREATE\s+TABLE\s+\[?(\w+)\]?\.\[?(\w+)\]?\s*\(";
            var tableMatch = Regex.Match(content, tablePattern, RegexOptions.IgnoreCase);

            if (!tableMatch.Success)
            {
                return null;
            }

            var schema = tableMatch.Groups[1].Value;
            var tableName = tableMatch.Groups[2].Value;

            if (!string.Equals(schema, "dbo", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"Skipped (nested schema [{schema}]): {Path.GetFileName(filePath)}");
                return null;
            }

            var columns = ParseColumns(content);

            return new TableSchema
            {
                Schema = schema,
                TableName = tableName,
                Columns = columns
            };
        }

        private List<ColumnSchema> ParseColumns(string content)
        {
            var columns = new List<ColumnSchema>();

            var tablePattern = @"CREATE\s+TABLE\s+\[?\w+\]?\.\[?\w+\]?\s*\(";
            var tableMatch = Regex.Match(content, tablePattern, RegexOptions.IgnoreCase);

            if (!tableMatch.Success)
            {
                return columns;
            }

            var startIndex = tableMatch.Index + tableMatch.Length;
            var body = ExtractTableBody(content, startIndex);

            if (string.IsNullOrEmpty(body))
            {
                return columns;
            }

            var lines = body.Split('\n');
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                // CONSTRAINT, PRIMARY KEY, FOREIGN KEY 등은 스킵
                if (string.IsNullOrEmpty(trimmedLine) ||
                    trimmedLine.StartsWith("CONSTRAINT", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("PRIMARY", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("FOREIGN", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("UNIQUE", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("INDEX", StringComparison.OrdinalIgnoreCase) ||
                    trimmedLine.StartsWith("--"))
                {
                    continue;
                }

                var column = ParseColumnDefinition(trimmedLine);
                if (column != null)
                {
                    columns.Add(column);
                }
            }

            return columns;
        }

        private string ExtractTableBody(string content, int startIndex)
        {
            var depth = 1; // 이미 첫 번째 '('를 지나왔으므로 1부터 시작
            var endIndex = startIndex;

            for (var i = startIndex; i < content.Length; i++)
            {
                if (content[i] == '(')
                {
                    depth++;
                }
                else if (content[i] == ')')
                {
                    depth--;
                    if (depth == 0)
                    {
                        endIndex = i;
                        break;
                    }
                }
            }

            if (depth != 0)
            {
                Debug.Assert(false, "Unmatched parentheses in SQL file");
                return string.Empty;
            }

            return content.Substring(startIndex, endIndex - startIndex);
        }

        private ColumnSchema? ParseColumnDefinition(string line)
        {
            // [ColumnName] DATATYPE(length) NULL/NOT NULL DEFAULT ...
            // 또는 [ColumnName] DATATYPE NULL/NOT NULL DEFAULT ...
            var columnPattern = @"^\[?(\w+)\]?\s+(\w+)(?:\(([^\)]+)\))?\s*(.*?)(?:,|$)";
            var match = Regex.Match(line, columnPattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return null;
            }

            var columnName = match.Groups[1].Value;
            var dataType = match.Groups[2].Value.ToUpper();
            var lengthPart = match.Groups[3].Value;
            var options = match.Groups[4].Value.ToUpper();

            if (dataType == "IDENTITY" || dataType == "DEFAULT" || dataType == "CONSTRAINT")
            {
                return null;
            }

            var isNullable = !options.Contains("NOT NULL");

            int? maxLength = null;
            int? precision = null;
            int? scale = null;

            if (!string.IsNullOrEmpty(lengthPart))
            {
                if (lengthPart.ToUpper() == "MAX")
                {
                    maxLength = -1; // MAX 표시
                }
                else if (lengthPart.Contains(","))
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

            return new ColumnSchema
            {
                ColumnName = columnName,
                DataType = dataType,
                IsNullable = isNullable,
                CharacterMaximumLength = maxLength,
                NumericPrecision = precision,
                NumericScale = scale
            };
        }
    }
}
