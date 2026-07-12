namespace PlayGround.Infrastructure.Database;

/// <summary>
/// 논리 DB 구분. Account(인증·신원) / Soccer(도메인)로 물리 분리.
/// 두 DB 간 FK·트랜잭션은 불가 — 정합성은 앱 계층에서 관리.
/// </summary>
public enum DatabaseTypes
{
    Account,
    Soccer
}
