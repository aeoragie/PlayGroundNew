namespace PlayGround.Shared.Time;

/// <summary>
/// 시간 이동 — 만료·마감처럼 "며칠 뒤"를 봐야 하는 로직을 기다리지 않고 확인한다.
/// DB의 `SystemClockOffset`과 **같은 값**이어야 한다(한쪽만 옮기면 앱과 DB의 "지금"이 어긋난다).
/// `#if DEBUG`라 RELEASE에는 오프셋을 담을 자리 자체가 없다. 배경은 CLAUDE.md "시간 이동".
/// </summary>
public static class DebugClock
{
#if DEBUG
    private static TimeSpan mOffset = TimeSpan.Zero;

    public static TimeSpan Offset => mOffset;

    public static void Shift(TimeSpan offset) => mOffset = offset;
#else
    public static TimeSpan Offset => TimeSpan.Zero;
#endif
}
