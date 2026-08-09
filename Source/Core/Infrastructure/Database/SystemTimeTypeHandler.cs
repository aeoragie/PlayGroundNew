using Dapper;
using PlayGround.Shared.Time;
using System.Data;
using System.Globalization;

namespace PlayGround.Infrastructure.Database;

public sealed class SystemTimeTypeHandler : SqlMapper.TypeHandler<SystemTime>
{
    public override SystemTime Parse(object value) => new((DateTime)value);

    public override void SetValue(IDbDataParameter parameter, SystemTime value)
    {
        parameter.DbType = DbType.DateTime2;
        parameter.Value = value.UtcDateTime;
    }
}

public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
{
    public override DateOnly Parse(object value) => value switch
    {
        DateTime dateTime => DateOnly.FromDateTime(dateTime),
        DateOnly dateOnly => dateOnly,
        _ => DateOnly.Parse(Convert.ToString(value, CultureInfo.InvariantCulture)!, CultureInfo.InvariantCulture),
    };

    public override void SetValue(IDbDataParameter parameter, DateOnly value)
    {
        parameter.DbType = DbType.Date;
        parameter.Value = value.ToDateTime(TimeOnly.MinValue);
    }
}
