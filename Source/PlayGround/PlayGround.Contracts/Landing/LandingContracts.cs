namespace PlayGround.Contracts.Landing
{
    public class LandingContentsResponse
    {
        public List<LandingItemDto> Features { get; set; } = new();
        public List<LandingItemDto> Steps { get; set; } = new();
    }

    public class LandingItemDto
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
    }
}
