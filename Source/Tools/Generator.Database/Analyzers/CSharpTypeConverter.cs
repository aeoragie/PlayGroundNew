namespace Generator.Database.Analyzers
{

    public static class CSharpTypeConverter
    {
        public enum ValueType
        {
            Default,
            Float,
            Boolean,
            String,
            Vector,
            TableType,
            DateTime,
            Guid,
        }

        public static (string CSharpType, ValueType ValueType) GetCSharpType(string mssqlType, string? defineType, bool isNullable = false)
        {
            var (csharpType, valueType) = GetBaseCSharpType(mssqlType, defineType);
            if (isNullable && valueType != ValueType.String && valueType != ValueType.Vector)
            {
                csharpType = $"{csharpType}?";
            }

            return (csharpType, valueType);
        }

        private static (string CSharpType, ValueType ValueType) GetBaseCSharpType(string mssqlType, string? defineType)
        {
            var normalizedType = mssqlType.ToLower().Trim();
            return normalizedType switch
            {
                // Integer Types
                "bigint" => ("long", ValueType.Default),
                "int" => ("int", ValueType.Default),
                "smallint" => ("short", ValueType.Default),
                "tinyint" => ("byte", ValueType.Default),

                // Double, Real, Float Types
                "float" => ("double", ValueType.Float),
                "real" => ("float", ValueType.Float),

                // Decimal, Numeric, Money, SmallMoney Types
                "decimal" => ("decimal", ValueType.Default),
                "numeric" => ("decimal", ValueType.Default),
                "money" => ("decimal", ValueType.Default),
                "smallmoney" => ("decimal", ValueType.Default),

                // Boolean Types
                "bit" => ("bool", ValueType.Boolean),

                // Date, DateTime, SmallDateTime, Time, DateTimeOffset Types
                "date" => ("DateTime", ValueType.DateTime),
                "datetime" => ("DateTime", ValueType.DateTime),
                "smalldatetime" => ("DateTime", ValueType.DateTime),
                "time" => ("TimeSpan", ValueType.Default),
                "datetimeoffset" => ("DateTimeOffset", ValueType.Default),

                // String Types (varchar)
                "char" => ("string", ValueType.String),
                "text" => ("string", ValueType.String),

                // String Types (nvarchar)
                "nchar" => ("string", ValueType.String),
                "nvarchar" => ("string", ValueType.String),
                "ntext" => ("string", ValueType.String),

                // Binary Types
                "binary" => ("byte[]", ValueType.Vector),
                "image" => ("byte[]", ValueType.Vector),
                "timestamp" => ("byte[]", ValueType.Vector),
                "rowversion" => ("byte[]", ValueType.Vector),

                // Guid Types
                "uniqueidentifier" => ("Guid", ValueType.Guid),

                // TableValueParameter Types
                "table type" => !string.IsNullOrEmpty(defineType)
                    ? ("SqlMapper.ICustomQueryParameter", ValueType.TableType)
                    : throw new NotImplementedException("Table type requires a defined user type"),

                // Other Types
                _ when normalizedType.StartsWith("varchar") => ("string", ValueType.String),
                _ when normalizedType.StartsWith("nvarchar") => ("string", ValueType.String),
                _ when normalizedType.StartsWith("nchar") => ("string", ValueType.String),
                _ when normalizedType.StartsWith("varbinary") => ("byte[]", ValueType.Vector),
                _ when normalizedType.StartsWith("datetime2") => ("DateTime", ValueType.DateTime),
                _ when normalizedType.StartsWith("char(") => ("string", ValueType.String),

                // Exception for unsupported types
                _ => throw new NotImplementedException($"SQL Server type '{mssqlType}' is not supported for C# conversion")
            };
        }

        public static string GetDefaultValue(ValueType valueType, bool isNullable = false)
        {
            if (isNullable && valueType != ValueType.String && valueType != ValueType.Vector)
            {
                return "null";
            }

            return valueType switch
            {
                ValueType.Default => "0",
                ValueType.Float => "0.0",
                ValueType.Boolean => "false",
                ValueType.String => "String.Empty",
                ValueType.Vector => "Array.Empty<byte>()",
                ValueType.TableType => "null",
                ValueType.DateTime => "DateTime.MinValue",
                ValueType.Guid => "Guid.Empty",
                _ => "default"
            };
        }

    }
}
