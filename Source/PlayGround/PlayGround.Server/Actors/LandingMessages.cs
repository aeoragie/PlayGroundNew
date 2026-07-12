namespace PlayGround.Server.Actors
{
    /// <summary>랜딩 콘텐츠 조회 요청 메시지. 액터 메시지는 불변이어야 하므로 record 사용.</summary>
    public sealed record GetLandingContentsRequest;
}
