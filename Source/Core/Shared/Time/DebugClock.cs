namespace PlayGround.Shared.Time;

/// <summary>
/// **시간 이동 — 디버그 빌드에서만 존재한다.** 만료·마감·재신청 제한처럼 "며칠 뒤"를 봐야 하는
/// 로직을 실제로 기다리지 않고 확인하려고 둔다.
///
/// <para>
/// **RELEASE 빌드에는 오프셋을 담을 자리가 아예 없다.** `#if DEBUG`라 필드도 setter도
/// 컴파일되지 않고, <see cref="Offset"/>은 항상 <see cref="TimeSpan.Zero"/>를 돌려주는 상수식이 된다.
/// 설정 실수로 운영 시계가 밀리는 경로가 물리적으로 존재하지 않는다 —
/// 런타임 플래그(`IsDevelopment()`)보다 강한 보장이다.
/// </para>
///
/// <para>
/// **DB와 같은 값을 써야 한다.** DB는 `dbo.UfnSystemDate()`가 `SystemClockOffset` 테이블을 읽어
/// 같은 만큼 옮긴다(디버그 배포본만). 한쪽만 옮기면 앱이 넘긴 시각과 DB의 "지금"이 어긋나
/// 테스트가 거짓 결과를 낸다. 서버 기동 시 그 테이블을 읽어 여기에 채운다.
/// </para>
/// </summary>
public static class DebugClock
{
#if DEBUG
    private static TimeSpan mOffset = TimeSpan.Zero;

    /// <summary>현재 적용 중인 시간 이동 폭. 기본 0.</summary>
    public static TimeSpan Offset => mOffset;

    /// <summary>DB의 `SystemClockOffset`과 같은 값으로 맞춘다. 디버그 빌드에만 존재한다.</summary>
    public static void Shift(TimeSpan offset) => mOffset = offset;
#else
    /// <summary>RELEASE에서는 언제나 0이다 — 오프셋을 담을 자리 자체가 없다.</summary>
    public static TimeSpan Offset => TimeSpan.Zero;
#endif
}
