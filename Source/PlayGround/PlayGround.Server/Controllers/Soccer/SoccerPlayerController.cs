using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
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

        [HttpGet("me/info")]
        public async Task<Envelope<PlayerInfoResponse>> GetMyInfoAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerInfoResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerInfoResponse> result = await mGateway.AskAsync<PlayerInfoResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerInfoMessage(userId), cancellation);
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
        public async Task<Envelope<PlayerCareerResponse>> GetMyCareerAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerCareerResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerCareerResponse> result = await mGateway.AskAsync<PlayerCareerResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerCareerMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>커리어 이력 저장(신규·수정). 본인 프로필만 — 소유 판정은 서버가 UserId로 한다.</summary>
        [HttpPut("me/career")]
        public async Task<Envelope<bool>> SaveMyCareerAsync(
            [FromBody] SavePlayerCareerRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SaveSoccerPlayerCareerMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>커리어 이력 삭제·복구(실행취소). 소프트 삭제라 되돌릴 수 있다.</summary>
        [HttpPost("me/career/delete")]
        public async Task<Envelope<bool>> DeleteMyCareerAsync(
            [FromBody] DeletePlayerCareerRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new DeleteSoccerPlayerCareerMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>포트폴리오 영상 저장(신규·수정). 링크는 유튜브만 — 서버가 정규화하고 썸네일을 파생한다.</summary>
        [HttpPut("me/portfolio")]
        public async Task<Envelope<bool>> SaveMyPortfolioVideoAsync(
            [FromBody] SavePlayerPortfolioVideoRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SaveSoccerPlayerPortfolioVideoMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>포트폴리오 영상 삭제·복구(실행취소).</summary>
        [HttpPost("me/portfolio/delete")]
        public async Task<Envelope<bool>> DeleteMyPortfolioVideoAsync(
            [FromBody] DeletePlayerPortfolioVideoRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new DeleteSoccerPlayerPortfolioVideoMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/portfolio")]
        public async Task<Envelope<PlayerPortfolioResponse>> GetMyPortfolioAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerPortfolioResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerPortfolioResponse> result = await mGateway.AskAsync<PlayerPortfolioResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerPortfolioMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/season-stats")]
        public async Task<Envelope<PlayerSeasonStatsResponse>> GetMySeasonStatsAsync(
            [FromQuery] int season, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<PlayerSeasonStatsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<PlayerSeasonStatsResponse> result = await mGateway.AskAsync<PlayerSeasonStatsResponse>(
                ActorNames.SoccerPlayerProfile, new GetSoccerPlayerSeasonStatsMessage(userId, season), cancellation);
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
            [FromBody] SetPlayerFieldVisibilityRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerPlayerProfile, new SetSoccerPlayerFieldVisibilityMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }
    }
}
