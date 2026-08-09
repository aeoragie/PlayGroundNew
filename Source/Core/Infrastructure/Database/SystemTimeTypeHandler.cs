using Dapper;
using PlayGround.Shared.Time;
using System.Data;
using System.Globalization;

namespace PlayGround.Infrastructure.Database;

/// <summary>
/// Dapper ↔ <see cref="SystemTime"/> 변환. DB의 datetime2는 Kind 없이 돌아오는데(Unspecified),
/// 저장 규칙상 전부 UTC이므로 읽는 즉시 UTC로 정규화해 감싼다. 쓸 때는 UTC `DateTime`으로 풀어 보낸다.
/// 로직 코드가 `new SystemTime(dbValue)`를 직접 할 필요가 없는 이유다 — 여기서 한 번에 처리된다.
/// 등록은 <see cref="Base.RepositoryBase"/> 정적 생성자(모든 쿼리가 그 경로를 지난다).
/// </summary>
public sealed class SystemTimeTypeHandler : SqlMapper.TypeHandler<SystemTime>
{
    public override SystemTime Parse(object value) => new((DateTime)value);

    public override void SetValue(IDbDataParameter parameter, SystemTime value)
    {
        parameter.DbType = DbType.DateTime2;
        parameter.Value = value.UtcDateTime;
    }
}

/// <summary>
/// Dapper ↔ <see cref="DateOnly"/> 변환. DATE 컬럼(생년월일·커리어 기간·대회 일정)은 순간이 아니라
/// 달력 날짜라 시간대 개념이 없다 — `DateOnly`로 받아야 "보는 시간대에 따라 하루 밀리는" 사고가 없다.
/// </summary>
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
