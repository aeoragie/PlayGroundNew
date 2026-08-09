namespace PlayGround.Shared.Time;

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
