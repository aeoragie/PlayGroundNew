using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlayGround.Application.Export.Commands;
using PlayGround.Contracts.Export;
using PlayGround.Infrastructure.Logging;
using PlayGround.Shared.Http;
using PlayGround.Shared.Result;
using System.Security.Claims;

namespace PlayGround.Server.Controllers.Soccer
{
    /// <summary>데이터 내려받기 (Design.SettingsFlows ③). 요청 접수(백그라운드 잡) · 상태 · 취소 · 서명 URL 다운로드.
    /// 생성은 동기로 하지 않는다 — 요청 API는 접수만 반환하고 워커가 파일을 만든다.</summary>
    [ApiController]
    [Route("api/soccer/exports")]
    public class SoccerExportController : ControllerBase
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly DataExportCommand mCommand;

        public SoccerExportController(DataExportCommand command)
        {
            mCommand = command;
        }

        private Guid CurrentUserId =>
            Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out Guid id) ? id : Guid.Empty;

        /// <summary>데이터 내려받기 요청 — 접수만 반환(Ok/InProgress/Cooldown). 파일 생성은 백그라운드.</summary>
        [Authorize]
        [HttpPost("me")]
        public async Task<Envelope<DataExportRequestResult>> RequestAsync(
            [FromBody] CreateDataExportRequest request, CancellationToken cancellation)
        {
            Result<DataExportRequestResult> result = await mCommand.RequestAsync(CurrentUserId, request, cancellation);
            if (result.IsError)
            {
                result.LogWith(Logger, "RequestDataExport");
            }

            return result.ToEnvelope();
        }

        /// <summary>현재 내려받기 상태 (없거나 만료면 null → 클라는 "요청" 버튼). 진행 중이면 폴링용으로 재조회.</summary>
        [Authorize]
        [HttpGet("me")]
        public async Task<Envelope<DataExportStateDto?>> GetCurrentAsync(CancellationToken cancellation)
        {
            Result<DataExportStateDto?> result = await mCommand.GetCurrentAsync(CurrentUserId, cancellation);
            return result.ToEnvelope();
        }

        [Authorize]
        [HttpPost("me/{requestId:guid}/cancel")]
        public async Task<Envelope<bool>> CancelAsync(Guid requestId, CancellationToken cancellation)
        {
            Result<bool> result = await mCommand.CancelAsync(CurrentUserId, requestId, cancellation);
            return result.ToEnvelope();
        }

        /// <summary>서명 URL 다운로드 — 토큰이 곧 자격(추측 불가). Ready·미만료·횟수&lt;상한을 SP가 원자 검증·증가한다.
        /// 조건 미충족(만료·초과·잘못된 토큰)이면 404 — 존재 여부를 흘리지 않는다.</summary>
        [AllowAnonymous]
        [HttpGet("download/{token}")]
        public async Task<IActionResult> DownloadAsync(string token, CancellationToken cancellation)
        {
            Result<Stream?> result = await mCommand.ResolveDownloadAsync(token, cancellation);
            if (result.IsError || result.Value is null)
            {
                return NotFound();
            }

            return File(result.Value, "application/zip", "playground-export.zip");
        }
    }
}
