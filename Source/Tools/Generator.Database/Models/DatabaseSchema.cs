namespace Generator.Database.Models
{
    public class DatabaseSchema
    {
        public string DatabaseName { get; set; } = string.Empty;
        public List<TableSchema> Tables { get; set; } = new List<TableSchema>();
        public List<ProcedureSchema> Procedures { get; set; } = new List<ProcedureSchema>();
        public List<QuerySchema> Queries { get; set; } = new List<QuerySchema>();
    }

    public class TableSchema
    {
        public string Schema { get; set; } = string.Empty;
        public string TableName { get; set; } = string.Empty;
        public List<ColumnSchema> Columns { get; set; } = new List<ColumnSchema>();
    }

    public class ColumnSchema
    {
        public string ColumnName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public string? UserDefinedType { get; set; }
        public bool IsNullable { get; set; }
        public int? CharacterMaximumLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
    }

    public class ProcedureSchema
    {
        public string Schema { get; set; } = string.Empty;
        public string ProcedureName { get; set; } = string.Empty;
        public List<ParameterSchema> Parameters { get; set; } = new List<ParameterSchema>();
        public string? EntityName { get; set; }
        public string? SourceType { get; set; }
        public List<JoinSource> JoinSources { get; set; } = new List<JoinSource>();
    }

    public class JoinSource
    {
        public string TableName { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new List<string>();
    }

    public class ParameterSchema
    {
        public string ParameterName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool HasDefault { get; set; }
        public string? DefaultValue { get; set; }
        public bool IsNullable { get; set; }
        public int? CharacterMaximumLength { get; set; }
        public int? NumericPrecision { get; set; }
        public int? NumericScale { get; set; }
    }

    public class QuerySchema
    {
        public string QueryName { get; set; } = string.Empty;
        public string SqlBody { get; set; } = string.Empty;
        public List<QueryParameterSchema> Parameters { get; set; } = new List<QueryParameterSchema>();
    }

    public class QueryParameterSchema
    {
        public string ParameterName { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsNullable { get; set; }
    }

    public class GeneratedFile
    {
        public string FileName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Database { get; set; } = string.Empty;
    }
}
