using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Contracts.Team;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 선수(본인 데이터). 온보딩 프로필 생성 등.</summary>
    [ApiController]
    [Route("api/soccer/player")]
    [Authorize]
    public class SoccerPlayerController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerPlayerController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        [HttpPost("me/profile")]
        public async Task<Envelope<CreatePlayerProfileResponse>> CreateMyProfileAsync(
            [FromBody] CreatePlayerProfileRequest request, CancellationToken cancellation)
        {
            // 인증 컨텍스트는 메일박스를 못 넘으므로 UserId(sub)를 메시지에 실어 보낸다
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<CreatePlayerProfileResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<CreatePlayerProfileResponse> result = await mGateway.AskAsync<CreatePlayerProfileResponse>(
                ActorNames.SoccerPlayerProfile, new CreatePlayerProfileMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>이 계정이 관리하는 선수(자녀) 목록. **보호자는 자녀가 여러 명일 수 있다** —
        /// 다른 조회는 playerId로 어느 자녀인지 지정한다(생략하면 첫 자녀).</summary>
        [HttpGet("me/players")]
        public async Task<Envelope<ManagedPlayersResponse>> GetMyPlayersAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<ManagedPlayersResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<ManagedPlayersResponse> result = await mGateway.AskAsync<ManagedPlayersResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerManagedPlayersMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/info")]
        public async Task<Envelope<PlayerInfoResponse>> GetMyInfoAsync([FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerInfoResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerInfoResponse> result = await mGateway.AskAsync<PlayerInfoResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerInfoMessage(userId, playerId), cancellation);
            return result.ToEnvelope();
        }

        [HttpPost("me/claim")]
        public async Task<Envelope<ClaimPlayerInviteResponse>> ClaimInviteAsync(
            [FromBody] ClaimPlayerInviteRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<ClaimPlayerInviteResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            // 현재 역할은 JWT 클레임에서 — General만 Player로 승격 (상위 역할 강등 방지)
            string? currentRole = User.FindFirstValue(ClaimTypes.Role);
            Result<ClaimPlayerInviteResponse> result = await mGateway.AskAsync<ClaimPlayerInviteResponse>(
                ActorNames.SoccerPlayerProfile, new ClaimSoccerPlayerInviteMessage(userId, currentRole, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/career")]
        public async Task<Envelope<PlayerCareerResponse>> GetMyCareerAsync([FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerCareerResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerCareerResponse> result = await mGateway.AskAsync<PlayerCareerResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerCareerMessage(userId, playerId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>커리어 이력 저장(신규·수정). 본인 프로필만 — 소유 판정은 서버가 UserId로 한다.</summary>
        [HttpPut("me/career")]
        public async Task<Envelope<bool>> SaveMyCareerAsync(
            [FromBody] SavePlayerCareerRequest request, [FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SaveSoccerPlayerCareerMessage(userId, request, playerId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>커리어 이력 삭제·복구(실행취소). 소프트 삭제라 되돌릴 수 있다.</summary>
        [HttpPost("me/career/delete")]
        public async Task<Envelope<bool>> DeleteMyCareerAsync(
            [FromBody] DeletePlayerCareerRequest request, [FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new DeleteSoccerPlayerCareerMessage(userId, request, playerId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>포트폴리오 영상 저장(신규·수정). 링크는 유튜브만 — 서버가 정규화하고 썸네일을 파생한다.</summary>
        [HttpPut("me/portfolio")]
        public async Task<Envelope<bool>> SaveMyPortfolioVideoAsync(
            [FromBody] SavePlayerPortfolioVideoRequest request, [FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SaveSoccerPlayerPortfolioVideoMessage(userId, request, playerId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>포트폴리오 영상 삭제·복구(실행취소).</summary>
        [HttpPost("me/portfolio/delete")]
        public async Task<Envelope<bool>> DeleteMyPortfolioVideoAsync(
            [FromBody] DeletePlayerPortfolioVideoRequest request, [FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new DeleteSoccerPlayerPortfolioVideoMessage(userId, request, playerId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/portfolio")]
        public async Task<Envelope<PlayerPortfolioResponse>> GetMyPortfolioAsync([FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerPortfolioResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerPortfolioResponse> result = await mGateway.AskAsync<PlayerPortfolioResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerPortfolioMessage(userId, playerId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/season-stats")]
        public async Task<Envelope<PlayerSeasonStatsResponse>> GetMySeasonStatsAsync(
            [FromQuery] int season, [FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerSeasonStatsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerSeasonStatsResponse> result = await mGateway.AskAsync<PlayerSeasonStatsResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerSeasonStatsMessage(userId, season, playerId), cancellation);
            return result.ToEnvelope();
        }

        // 공개 선수 프로필 — 비로그인 읽기전용. 'me' 리터럴 라우트가 {slug}보다 우선 매칭된다.
        // 미존재·프로필 비공개는 NotFound 하나 — 비공개 여부를 흘리지 않는다.
        // 로그인 열람자면 UserId를 실어 보낸다 — 승인된 에이전트 판정(권한 뷰)에만 쓴다 (공개홈 IsManager 패턴).
        [AllowAnonymous]
        [HttpGet("{slug}/profile")]
        public async Task<Envelope<PlayerPublicProfileResponse>> GetPublicProfileAsync(
            string slug, [FromQuery] int season, CancellationToken cancellation)
        {
            Guid? viewerUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;
            Result<PlayerPublicProfileResponse> result = await mGateway.AskAsync<PlayerPublicProfileResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerPublicProfileMessage(slug, season, viewerUserId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>선수 사진 설정·삭제. 대상이 본인 프로필이 아닐 수 있어(팀 관리자 경로) me/ 아래에 두지 않는다.
        /// 권한(보호자·소속팀 관리자)은 서버가 판정하며 거부는 403 — 선수 존재 여부는 흘리지 않는다.</summary>
        [HttpPut("photo")]
        public async Task<Envelope<bool>> SetPlayerPhotoAsync(
            [FromBody] SetPlayerPhotoRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SetSoccerPlayerPhotoMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpPut("me/profile/visibility")]
        public async Task<Envelope<bool>> SetMyFieldVisibilityAsync(
            [FromBody] SetPlayerFieldVisibilityRequest request, [FromQuery] Guid? playerId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SetSoccerPlayerFieldVisibilityMessage(userId, request, playerId), cancellation);
            return result.ToEnvelope();
        }

        //.// 보호자 기록 수정 신청 — 내 자녀 관련 공식 경기만. 심사·반영은 주최측(설계 결정 7).

        /// <summary>보호자 기록 수정 신청 생성. playerId = 어느 자녀인지.</summary>
        [HttpPost("me/corrections")]
        public async Task<Envelope<Guid>> CreateMyGuardianCorrectionAsync(
            [FromBody] CreateRecordCorrectionRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<Guid>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Guid targetPlayerId = request.TargetPlayerId ?? Guid.Empty;
            Result<Guid> result = await mGateway.AskAsync<Guid>(
                ActorNames.SoccerPlayerProfile, new CreateSoccerGuardianCorrectionMessage(userId, targetPlayerId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>내가 올린 기록 수정 신청 목록(보호자). 화면이 자녀별로 걸러 보여준다.</summary>
        [HttpGet("me/corrections")]
        public async Task<Envelope<RecordCorrectionsResponse>> GetMyGuardianCorrectionsAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<RecordCorrectionsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<RecordCorrectionsResponse> result = await mGateway.AskAsync<RecordCorrectionsResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerGuardianCorrectionsMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>보호자 기록 수정 신청 취소 — 접수(Pending) 상태의 내 신청만.</summary>
        [HttpDelete("me/corrections/{correctionId:guid}")]
        public async Task<Envelope<bool>> CancelMyGuardianCorrectionAsync(Guid correctionId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new CancelSoccerGuardianCorrectionMessage(userId, correctionId), cancellation);
            return result.ToEnvelope();
        }
    }
}
