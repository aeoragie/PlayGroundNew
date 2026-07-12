namespace PlayGround.Contracts.Landing
{
    /// <summary>랜딩 페이지 콘텐츠 응답 (핵심 기능 / 작동 방식 3스텝)</summary>
    public class LandingContentsResponse
    {
        public List<LandingItemDto> Features { get; set; } = new();
        public List<LandingItemDto> Steps { get; set; } = new();
    }

    /// <summary>랜딩 카드 하나 (아이콘/번호 + 제목 + 본문)</summary>
    public class LandingItemDto
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
