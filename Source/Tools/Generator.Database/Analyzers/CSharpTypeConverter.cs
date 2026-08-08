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
            Date,
            Guid,
        }

        public static (string CSharpType, ValueType ValueType) GetCSharpType(string mssqlType, string? defineType, bool isNullable = false)
        {
            var (csharpType, valueType) = GetBaseCSharpType(mssqlType, defineType);

            // 참조형(string·byte[])도 NULL 허용이면 `?`를 붙인다 — 생성 프로젝트가 nullable enable이라
            // 안 붙이면 기본값 null 대입이 CS8625로 샌다. 널 허용 여부는 SQL이 진실이다.
            if (isNullable)
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
                "bigint" => ("long", ValueType.Default),
                "int" => ("int", ValueType.Default),
                "smallint" => ("short", ValueType.Default),
                "tinyint" => ("byte", ValueType.Default),

                "float" => ("double", ValueType.Float),
                "real" => ("float", ValueType.Float),

                // Decimal, Numeric, Money, SmallMoney Types
                "decimal" => ("decimal", ValueType.Default),
                "numeric" => ("decimal", ValueType.Default),
                "money" => ("decimal", ValueType.Default),
                "smallmoney" => ("decimal", ValueType.Default),

                "bit" => ("bool", ValueType.Boolean),

                // Date, DateTime, SmallDateTime, Time, DateTimeOffset Types
                // 순간(datetime 계열)은 SystemTime(UTC 강제), 달력 날짜(date)는 DateOnly —
                // 원시 DateTime을 생성물에 남기지 않는다 (TimeBaselineGuardTests가 로직 코드에서 금지)
                "date" => ("DateOnly", ValueType.Date),
                "datetime" => ("SystemTime", ValueType.DateTime),
                "smalldatetime" => ("SystemTime", ValueType.DateTime),
                "time" => ("TimeSpan", ValueType.Default),
                "datetimeoffset" => ("DateTimeOffset", ValueType.Default),

                "char" => ("string", ValueType.String),
                "text" => ("string", ValueType.String),

                "nchar" => ("string", ValueType.String),
                "nvarchar" => ("string", ValueType.String),
                "ntext" => ("string", ValueType.String),

                "binary" => ("byte[]", ValueType.Vector),
                "image" => ("byte[]", ValueType.Vector),
                "timestamp" => ("byte[]", ValueType.Vector),
                "rowversion" => ("byte[]", ValueType.Vector),

                "uniqueidentifier" => ("Guid", ValueType.Guid),

                "table type" => !string.IsNullOrEmpty(defineType)
                    ? ("SqlMapper.ICustomQueryParameter", ValueType.TableType)
                    : throw new NotImplementedException("Table type requires a defined user type"),

                _ when normalizedType.StartsWith("varchar") => ("string", ValueType.String),
                _ when normalizedType.StartsWith("nvarchar") => ("string", ValueType.String),
                _ when normalizedType.StartsWith("nchar") => ("string", ValueType.String),
                _ when normalizedType.StartsWith("varbinary") => ("byte[]", ValueType.Vector),
                _ when normalizedType.StartsWith("datetime2") => ("SystemTime", ValueType.DateTime),
                _ when normalizedType.StartsWith("char(") => ("string", ValueType.String),

                _ => throw new NotImplementedException($"SQL Server type '{mssqlType}' is not supported for C# conversion")
            };
        }

        public static string GetDefaultValue(ValueType valueType, bool isNullable = false)
        {
            if (isNullable)
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
                ValueType.DateTime => "SystemTime.MinValue",
                ValueType.Date => "DateOnly.MinValue",
                ValueType.Guid => "Guid.Empty",
                _ => "default"
            };
        }

    }
}
