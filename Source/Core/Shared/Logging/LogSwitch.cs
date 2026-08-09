namespace PlayGround.Shared.Logging
{
    // 기반 라이브러리의 진단 로그 스위치. 켤 때는 값을 바꿔 다시 빌드한다.
    // const라서 꺼져 있으면 if 블록이 통째로 사라진다 — 인자 계산 비용까지 없어진다는 게 요점이고,
    // 런타임 IsEnabled 로는 인자가 이미 평가된 뒤라 막지 못한다.
    public static class LogSwitch
    {
        public const bool Database = false;

        public const bool Redis = false;

        public const bool Actor = false;

        public const bool Http = false;
    }
}
