namespace PlayGround.Domain.Soccer
{
    /// <summary>팀 게시판 글 유형 (Design.TeamBoard). 멤버 이름 = DB 저장 문자열 (SoccerTeamPosts.Type).
    /// 2종만 — 공지(Notice)·자료(Material). **공지만 로스터 보호자 알림**(자료는 알림 없음 — 알림 피로 방지).</summary>
    public enum SoccerTeamPostType
    {
        Notice,
        Material,
    }
}
