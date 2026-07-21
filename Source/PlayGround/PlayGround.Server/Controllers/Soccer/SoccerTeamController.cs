using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Team;
using PlayGround.Server.Actors;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>축구 팀(본인 운영). 온보딩 팀 생성 등.</summary>
    [ApiController]
    [Route("api/soccer/team")]
    [Authorize]
    public class SoccerTeamController : ControllerBase
    {
        private readonly ActorGateway mGateway;

        public SoccerTeamController(ActorGateway gateway)
        {
            mGateway = gateway;
        }

        [HttpPost("me")]
        public async Task<Envelope<CreateTeamResponse>> CreateMyTeamAsync(
            [FromBody] CreateTeamRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<CreateTeamResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<CreateTeamResponse> result = await mGateway.AskAsync<CreateTeamResponse>(
                ActorNames.SoccerTeamProfile, new CreateSoccerTeamMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/info")]
        public async Task<Envelope<TeamInfoResponse>> GetMyTeamInfoAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamInfoResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamInfoResponse> result = await mGateway.AskAsync<TeamInfoResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamInfoMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        // 팀 탐색 공개 목록 — 비로그인. 검색 리소스 규칙(api/{sport}/teams)이라 복수형 절대 경로.
        [AllowAnonymous]
        [HttpGet("~/api/soccer/teams")]
        public async Task<Envelope<TeamExploreResponse>> GetExploreTeamsAsync(CancellationToken cancellation)
        {
            Result<TeamExploreResponse> result = await mGateway.AskAsync<TeamExploreResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamExploreMessage(), cancellation);
            return result.ToEnvelope();
        }

        // 공개 팀 홈페이지 — 비로그인 읽기전용. 'me' 리터럴 라우트가 {slug}보다 우선 매칭된다.
        // 로그인 열람자면 UserId를 실어 보낸다 — 관리자 본인 판정(IsManager, "관리" 텍스트 링크)에만 쓴다.
        [AllowAnonymous]
        [HttpGet("{slug}/home")]
        public async Task<Envelope<TeamPublicHomeResponse>> GetTeamHomeAsync(string slug, CancellationToken cancellation)
        {
            Guid? viewerUserId = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : null;
            Result<TeamPublicHomeResponse> result = await mGateway.AskAsync<TeamPublicHomeResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamHomeMessage(slug, viewerUserId), cancellation);
            return result.ToEnvelope();
        }

        // 공개 팀 홈 시즌성적 탭 — 비로그인 읽기전용. 탭 진입 시 지연 로드.
        [AllowAnonymous]
        [HttpGet("{slug}/season-record")]
        public async Task<Envelope<TeamSeasonRecordResponse>> GetTeamSeasonRecordAsync(
            string slug, [FromQuery] int season, CancellationToken cancellation)
        {
            Result<TeamSeasonRecordResponse> result = await mGateway.AskAsync<TeamSeasonRecordResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamSeasonRecordMessage(slug, season), cancellation);
            return result.ToEnvelope();
        }

        // 공개 팀 홈 모집 탭 — 비로그인 읽기전용. 탭 진입 시 지연 로드.
        [AllowAnonymous]
        [HttpGet("{slug}/recruitments")]
        public async Task<Envelope<TeamRecruitmentsResponse>> GetTeamRecruitmentsAsync(string slug, CancellationToken cancellation)
        {
            Result<TeamRecruitmentsResponse> result = await mGateway.AskAsync<TeamRecruitmentsResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamRecruitmentsMessage(slug), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/recruitments")]
        public async Task<Envelope<TeamRecruitmentsResponse>> GetMyRecruitmentsAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamRecruitmentsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamRecruitmentsResponse> result = await mGateway.AskAsync<TeamRecruitmentsResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamRecruitmentsByManagerMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>모집 공고 저장 (신규·수정 겸용) — 저장 즉시 공개 홈 모집 탭·팀 탐색 모집중 뱃지에 반영된다.</summary>
        [HttpPost("me/recruitments")]
        public async Task<Envelope<TeamRecruitmentDto>> SaveMyRecruitmentAsync(
            [FromBody] SaveTeamRecruitmentRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamRecruitmentDto> result = await mGateway.AskAsync<TeamRecruitmentDto>(
                ActorNames.SoccerTeamProfile, new SaveSoccerTeamRecruitmentMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>모집 공고 마감 — Open → Closed 단방향 (재오픈 없음, 새 모집은 새 공고로).</summary>
        [HttpPost("me/recruitments/{recruitmentId:guid}/close")]
        public async Task<Envelope<TeamRecruitmentDto>> CloseMyRecruitmentAsync(Guid recruitmentId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamRecruitmentDto>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamRecruitmentDto> result = await mGateway.AskAsync<TeamRecruitmentDto>(
                ActorNames.SoccerTeamProfile, new CloseSoccerTeamRecruitmentMessage(userId, recruitmentId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>모집 공고 소프트 삭제·복구(restore=true — 실행취소 경로).</summary>
        [HttpPost("me/recruitments/{recruitmentId:guid}/delete")]
        public async Task<Envelope<bool>> DeleteMyRecruitmentAsync(
            Guid recruitmentId, [FromQuery] bool restore, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerTeamProfile, new DeleteSoccerTeamRecruitmentMessage(userId, recruitmentId, restore), cancellation);
            return result.ToEnvelope();
        }

        // 공개 팀 홈 진학·진로 탭 — 비로그인 읽기전용. 탭 진입 시 지연 로드.
        [AllowAnonymous]
        [HttpGet("{slug}/career-outcomes")]
        public async Task<Envelope<TeamCareerOutcomesResponse>> GetTeamCareerOutcomesAsync(string slug, CancellationToken cancellation)
        {
            Result<TeamCareerOutcomesResponse> result = await mGateway.AskAsync<TeamCareerOutcomesResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamCareerOutcomesMessage(slug), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/career-outcomes")]
        public async Task<Envelope<TeamCareerOutcomesResponse>> GetMyCareerOutcomesAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamCareerOutcomesResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamCareerOutcomesResponse> result = await mGateway.AskAsync<TeamCareerOutcomesResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamCareerOutcomesByManagerMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>진학·진로 사례 저장 (신규·수정 겸용) — 저장 즉시 공개 홈 진학·진로 탭에 반영된다.</summary>
        [HttpPost("me/career-outcomes")]
        public async Task<Envelope<TeamCareerOutcomeDto>> SaveMyCareerOutcomeAsync(
            [FromBody] SaveTeamCareerOutcomeRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamCareerOutcomeDto>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamCareerOutcomeDto> result = await mGateway.AskAsync<TeamCareerOutcomeDto>(
                ActorNames.SoccerTeamProfile, new SaveSoccerTeamCareerOutcomeMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>진학·진로 사례 소프트 삭제·복구(restore=true — 실행취소 경로).</summary>
        [HttpPost("me/career-outcomes/{outcomeId:guid}/delete")]
        public async Task<Envelope<bool>> DeleteMyCareerOutcomeAsync(
            Guid outcomeId, [FromQuery] bool restore, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerTeamProfile, new DeleteSoccerTeamCareerOutcomeMessage(userId, outcomeId, restore), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/roster")]
        public async Task<Envelope<TeamRosterResponse>> GetMyTeamRosterAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamRosterResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamRosterResponse> result = await mGateway.AskAsync<TeamRosterResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamRosterMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/matches")]
        public async Task<Envelope<TeamMatchesResponse>> GetMyTeamMatchesAsync(
            [FromQuery] int season, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamMatchesResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamMatchesResponse> result = await mGateway.AskAsync<TeamMatchesResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamMatchesMessage(userId, season), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>팀 정보 수정 — 저장 즉시 공개 홈페이지에 반영된다(같은 테이블을 읽는다).</summary>
        [HttpPut("me/info")]
        public async Task<Envelope<UpdateTeamInfoResponse>> UpdateMyTeamInfoAsync(
            [FromBody] UpdateTeamInfoRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<UpdateTeamInfoResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<UpdateTeamInfoResponse> result = await mGateway.AskAsync<UpdateTeamInfoResponse>(
                ActorNames.SoccerTeamProfile, new UpdateSoccerTeamInfoMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>결과 입력 폼의 대회/리그 선택지.</summary>
        [HttpGet("me/tournament-options")]
        public async Task<Envelope<TeamTournamentOptionsResponse>> GetMyTournamentOptionsAsync(
            [FromQuery] int season, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamTournamentOptionsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamTournamentOptionsResponse> result = await mGateway.AskAsync<TeamTournamentOptionsResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTournamentOptionsMessage(userId, season), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>경기 결과 입력. 대회 경기면 저장 시 순위표가 함께 재계산된다(D5).</summary>
        [HttpPost("me/matches")]
        public async Task<Envelope<CreateTeamMatchResultResponse>> CreateMyTeamMatchAsync(
            [FromBody] CreateTeamMatchResultRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<CreateTeamMatchResultResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<CreateTeamMatchResultResponse> result = await mGateway.AskAsync<CreateTeamMatchResultResponse>(
                ActorNames.SoccerTeamProfile, new CreateSoccerMatchResultMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>공식 기록 수정 신청 생성. 남의 경기·친선경기·중복 신청은 403 — 사유를 구분해 알리지 않는다.</summary>
        /// <remarks>
        /// **심사·반영 엔드포인트는 여기에 만들지 않는다** — 주최측(대회 운영 서비스)이 DB를 공유해 처리한다
        /// (설계 결정 6·7). PlayGround가 제공하는 것은 생성·조회·취소뿐이다.
        /// </remarks>
        [HttpPost("me/corrections")]
        public async Task<Envelope<Guid>> CreateMyRecordCorrectionAsync(
            [FromBody] CreateRecordCorrectionRequest request, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<Guid>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<Guid> result = await mGateway.AskAsync<Guid>(
                ActorNames.SoccerTeamProfile, new CreateSoccerRecordCorrectionMessage(userId, request), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>"처리가 필요해요" 항목 (대시보드 허브).
        /// **알림 테이블이 아니라 현재 상태에서 파생한다** — 읽음 상태가 없고, 처리하면 사라진다.</summary>
        [HttpGet("me/action-items")]
        public async Task<Envelope<ActionItemsResponse>> GetMyActionItemsAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<ActionItemsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<ActionItemsResponse> result = await mGateway.AskAsync<ActionItemsResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerActionItemsMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>내가 올린 기록 수정 신청 목록.</summary>
        [HttpGet("me/corrections")]
        public async Task<Envelope<RecordCorrectionsResponse>> GetMyRecordCorrectionsAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<RecordCorrectionsResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<RecordCorrectionsResponse> result = await mGateway.AskAsync<RecordCorrectionsResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerRecordCorrectionsMessage(userId), cancellation);
            return result.ToEnvelope();
        }

        /// <summary>신청 취소 — 접수 상태의 내 신청만. 심사가 끝난 건은 취소할 수 없다.</summary>
        [HttpPost("me/corrections/{correctionId:guid}/cancel")]
        public async Task<Envelope<bool>> CancelMyRecordCorrectionAsync(
            Guid correctionId, CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<bool>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<bool> result = await mGateway.AskAsync<bool>(
                ActorNames.SoccerTeamProfile, new CancelSoccerRecordCorrectionMessage(userId, correctionId), cancellation);
            return result.ToEnvelope();
        }

        [HttpGet("me/videos")]
        public async Task<Envelope<TeamVideosResponse>> GetMyTeamVideosAsync(CancellationToken cancellation)
        {
            string? sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!Guid.TryParse(sub, out Guid userId))
            {
                return Result<TeamVideosResponse>.Error(ErrorCode.Unauthorized, "Invalid subject").ToEnvelope();
            }

            Result<TeamVideosResponse> result = await mGateway.AskAsync<TeamVideosResponse>(
                ActorNames.SoccerTeamInfo, new GetSoccerTeamVideosMessage(userId), cancellation);
            return result.ToEnvelope();
        }
    }
}
