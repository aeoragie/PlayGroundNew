using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PlayGround.Shared.Result;
using PlayGround.Shared.Time;
using PlayGround.Contracts.Export;
using PlayGround.Contracts.Player;
using PlayGround.Contracts.Settings;
using PlayGround.Contracts.Team;
using PlayGround.Domain.Soccer;
using PlayGround.Domain.Time;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;

namespace PlayGround.Application.Export.Commands
{
    /// <summary>데이터 내려받기 유즈케이스 (Design.SettingsFlows ③). 요청 접수(즉시 반환) · 현재 상태 · 취소 ·
    /// **백그라운드 생성**(JSON+CSV zip → 비공개 저장 → Ready 전환 → 알림 센터+이메일) · 서명 URL 다운로드.
    /// 자녀 데이터는 연결된 자녀분만(GetManagedPlayers 스코프). 사진·영상 원본은 제외(URL만 JSON에 남는다).</summary>
    public class DataExportCommand
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly ISoccerDataExportRepository mExportRepository;
        private readonly IDataExportQueue mQueue;
        private readonly IAccountRepository mAccountRepository;
        private readonly IPlayerRepository mPlayerRepository;
        private readonly ISoccerTeamRepository mTeamRepository;
        private readonly IExportStorage mStorage;
        private readonly INotificationRepository mNotificationRepository;
        private readonly IEmailSender mEmailSender;

        public DataExportCommand(
            ISoccerDataExportRepository exportRepository,
            IDataExportQueue queue,
            IAccountRepository accountRepository,
            IPlayerRepository playerRepository,
            ISoccerTeamRepository teamRepository,
            IExportStorage storage,
            INotificationRepository notificationRepository,
            IEmailSender emailSender)
        {
            mExportRepository = exportRepository ?? throw new ArgumentNullException(nameof(exportRepository));
            mQueue = queue ?? throw new ArgumentNullException(nameof(queue));
            mAccountRepository = accountRepository ?? throw new ArgumentNullException(nameof(accountRepository));
            mPlayerRepository = playerRepository ?? throw new ArgumentNullException(nameof(playerRepository));
            mTeamRepository = teamRepository ?? throw new ArgumentNullException(nameof(teamRepository));
            mStorage = storage ?? throw new ArgumentNullException(nameof(storage));
            mNotificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            mEmailSender = emailSender ?? throw new ArgumentNullException(nameof(emailSender));
        }

        //.// 사용자 경로 — 접수(즉시 반환) · 상태 · 취소

        public async Task<Result<DataExportRequestResult>> RequestAsync(
            Guid userId, CreateDataExportRequest request, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || request is null)
            {
                return Result<DataExportRequestResult>.Error(ErrorCode.Unauthorized, "userId/request required");
            }

            // 최소 1개 항목은 포함해야 한다
            if (!request.IncludeProfile && !request.IncludeRecords && !request.IncludeRequests)
            {
                return Result<DataExportRequestResult>.Error(ErrorCode.InvalidInput, "select at least one item");
            }

            Result<(string Status, Guid? RequestId)> created = await mExportRepository.CreateAsync(
                userId, request.IncludeProfile, request.IncludeRecords, request.IncludeRequests, cancellation);
            if (created.IsError)
            {
                return Result<DataExportRequestResult>.Failure(created.ResultData);
            }

            // 접수됐으면 백그라운드 잡 큐에 넣고 즉시 반환(동기 생성 금지)
            if (created.Value.Status == "Ok" && created.Value.RequestId is { } id)
            {
                mQueue.Enqueue(id);
            }

            Result<DataExportStateDto?> current = await mExportRepository.GetByUserAsync(userId, cancellation);
            return Result<DataExportRequestResult>.Success(new DataExportRequestResult
            {
                Status = created.Value.Status,
                Export = current.IsError ? null : current.Value
            });
        }

        public async Task<Result<DataExportStateDto?>> GetCurrentAsync(Guid userId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty)
            {
                return Result<DataExportStateDto?>.Error(ErrorCode.Unauthorized, "userId is empty");
            }

            return await mExportRepository.GetByUserAsync(userId, cancellation);
        }

        public async Task<Result<bool>> CancelAsync(Guid userId, Guid requestId, CancellationToken cancellation = default)
        {
            if (userId == Guid.Empty || requestId == Guid.Empty)
            {
                return Result<bool>.Error(ErrorCode.InvalidInput, "userId/requestId required");
            }

            Result<bool> cancelled = await mExportRepository.CancelAsync(userId, requestId, cancellation);
            if (cancelled.IsError)
            {
                return cancelled;
            }

            return cancelled.Value
                ? Result<bool>.Success(true)
                : Result<bool>.Error(ErrorCode.Forbidden, "not cancellable");
        }

        //.// 다운로드 — 서명 URL 소비(Ready·미만료·횟수 검증은 SP) → 파일 스트림

        public async Task<Result<Stream?>> ResolveDownloadAsync(string token, CancellationToken cancellation = default)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return Result<Stream?>.Error(ErrorCode.InvalidInput, "token is required");
            }

            Result<string?> key = await mExportRepository.ConsumeDownloadAsync(token, cancellation);
            if (key.IsError)
            {
                return Result<Stream?>.Failure(key.ResultData);
            }

            if (string.IsNullOrEmpty(key.Value))
            {
                // 만료·횟수 초과·잘못된 토큰 — 다 같은 빈 결과(존재 여부 미노출)
                return Result<Stream?>.Success(null);
            }

            Stream? stream = await mStorage.OpenReadAsync(key.Value, cancellation);
            return Result<Stream?>.Success(stream);
        }

        //.// 백그라운드 생성 — 워커가 호출. 실패해도 예외를 밖으로 던지지 않고 Failed로 기록한다.

        public async Task GenerateAsync(Guid requestId, CancellationToken cancellation = default)
        {
            Result<DataExportJob?> jobResult = await mExportRepository.GetByIdAsync(requestId, cancellation);
            if (jobResult.IsError || jobResult.Value is null || jobResult.Value.Status != "Pending")
            {
                // 취소됐거나 이미 처리됨 — 조용히 종료
                return;
            }

            DataExportJob job = jobResult.Value;

            try
            {
                byte[] zipBytes = await BuildZipAsync(job, cancellation);

                using var content = new MemoryStream(zipBytes, writable: false);
                string storageKey = await mStorage.SaveAsync(requestId, content, cancellation);

                string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
                SystemTime expiresAt = SystemTime.Now.AddDays(7);

                Result<bool> updated = await mExportRepository.UpdateStatusAsync(
                    requestId, "Ready", token, storageKey, zipBytes.LongLength, expiresAt, cancellation);

                // Pending이 아니면(취소 등) 전환이 안 된다 — 그 경우 알림/이메일도 보내지 않는다
                if (updated.IsError || !updated.Value)
                {
                    return;
                }

                await NotifyReadyAsync(job.UserId, requestId, cancellation);
            }
            catch
            {
                // 실패로 기록하고 예외를 다시 던진다 — 워커(Server)가 로깅한다(Application은 NLog 미참조)
                await mExportRepository.UpdateStatusAsync(requestId, "Failed", null, null, null, null, cancellation);
                throw;
            }
        }

        /// <summary>완료 알림 — 알림 센터 + 이메일 두 채널. 둘 다 부가 작업이라 실패해도 생성 결과에 영향 없다.</summary>
        private async Task NotifyReadyAsync(Guid userId, Guid requestId, CancellationToken cancellation)
        {
            try
            {
                await mNotificationRepository.CreateAsync(
                    userId, SoccerNotificationType.ExportReady.ToString(), requestId, targetPlayerId: null,
                    actorName: null, playerName: null, teamName: null, metaText: null, subText: null, cancellation);
            }
            catch
            {
                // 알림 발송 실패는 부가 작업 — 파일은 이미 준비됐으므로 삼킨다
            }

            try
            {
                Result<AccountUser?> user = await mAccountRepository.GetByIdAsync(userId, cancellation);
                if (!user.IsError && user.Value is not null && !string.IsNullOrWhiteSpace(user.Value.Email))
                {
                    await mEmailSender.SendAsync(
                        user.Value.Email,
                        "요청하신 데이터 파일이 준비됐어요",
                        "PlayGround에서 요청하신 데이터 내려받기 파일이 준비됐어요. 설정 · 계정 탭에서 7일 안에 내려받을 수 있어요.",
                        cancellation);
                }
            }
            catch
            {
                // 이메일 발송 실패도 부가 작업 — 알림 센터로도 나가므로 삼킨다
            }
        }

        //.// 페이로드 조립 — 포함 항목별로 파일을 담아 zip. 사진·영상은 URL만(원본 제외).

        private async Task<byte[]> BuildZipAsync(DataExportJob job, CancellationToken cancellation)
        {
            using var buffer = new MemoryStream();
            using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
            {
                await WriteJsonAsync(zip, "README.txt", BuildReadme(), cancellation);

                if (job.IncludeProfile)
                {
                    Result<AccountSettingsResponse?> settings = await mAccountRepository.GetSettingsAsync(job.UserId, cancellation);
                    Result<AccountUser?> user = await mAccountRepository.GetByIdAsync(job.UserId, cancellation);
                    var account = new
                    {
                        displayName = user.Value?.DisplayName,
                        email = user.Value?.Email,
                        role = user.Value?.UserRole,
                        authProvider = user.Value?.AuthProvider,
                        socialLogins = settings.Value?.SocialLogins
                    };
                    await WriteObjectAsync(zip, "account/profile.json", account, cancellation);
                }

                // 연결된 자녀분만 (GetManagedPlayers 스코프)
                Result<ManagedPlayersResponse> children = await mPlayerRepository.GetManagedPlayersAsync(job.UserId, cancellation);
                var childList = children.IsError ? new List<ManagedPlayerDto>() : children.Value.Players;

                if (job.IncludeProfile || job.IncludeRecords)
                {
                    var csv = new StringBuilder();
                    csv.AppendLine("선수,연령,소속팀,출전,득점,도움");
                    var childData = new List<object>();

                    int season = KoreanTime.CurrentYear;
                    foreach (ManagedPlayerDto child in childList)
                    {
                        object? info = null;
                        object? careers = null;
                        int apps = 0, goals = 0, assists = 0;

                        if (job.IncludeProfile)
                        {
                            Result<PlayerInfoResponse?> infoResult = await mPlayerRepository.GetInfoByUserAsync(job.UserId, child.PlayerId, cancellation);
                            info = infoResult.Value;
                        }

                        if (job.IncludeRecords)
                        {
                            Result<PlayerCareerResponse> careerResult = await mPlayerRepository.GetCareersByUserAsync(job.UserId, child.PlayerId, cancellation);
                            careers = careerResult.IsError ? null : careerResult.Value;

                            Result<PlayerSeasonStatsResponse> statsResult = await mPlayerRepository.GetSeasonStatsByUserAsync(job.UserId, season, child.PlayerId, cancellation);
                            if (!statsResult.IsError)
                            {
                                apps = statsResult.Value.Matches.Count;
                                goals = statsResult.Value.Matches.Sum(m => m.Goals);
                                assists = statsResult.Value.Matches.Sum(m => m.Assists);
                                childData.Add(new { child.PlayerId, child.Name, info, careers, seasonStats = statsResult.Value });
                                csv.AppendLine($"{Csv(child.Name)},{Csv(child.AgeGroup)},{Csv(child.TeamName)},{apps},{goals},{assists}");
                                continue;
                            }
                        }

                        childData.Add(new { child.PlayerId, child.Name, info, careers });
                        csv.AppendLine($"{Csv(child.Name)},{Csv(child.AgeGroup)},{Csv(child.TeamName)},{apps},{goals},{assists}");
                    }

                    await WriteObjectAsync(zip, "children/children.json", childData, cancellation);
                    await WriteJsonAsync(zip, "children/summary.csv", "﻿" + csv, cancellation); // BOM — 엑셀 한글
                }

                if (job.IncludeRequests)
                {
                    Result<MyApplicationsResponse> applications = await mTeamRepository.GetApplicationsByGuardianAsync(job.UserId, cancellation);
                    await WriteObjectAsync(zip, "requests/applications.json",
                        applications.IsError ? new MyApplicationsResponse() : applications.Value, cancellation);
                }
            }

            return buffer.ToArray();
        }

        private static string BuildReadme() =>
            "PlayGround 데이터 내보내기\n\n" +
            "- account/profile.json : 계정·프로필\n" +
            "- children/children.json : 자녀별 프로필·커리어·시즌 기록\n" +
            "- children/summary.csv : 자녀 요약(엑셀에서 열 수 있어요)\n" +
            "- requests/applications.json : 지원·신청 내역\n\n" +
            "사진·영상 원본은 포함되지 않으며, 링크(URL)만 기록됩니다.\n";

        private static async Task WriteObjectAsync(ZipArchive zip, string path, object payload, CancellationToken cancellation)
        {
            string json = JsonSerializer.Serialize(payload, JsonOptions);
            await WriteJsonAsync(zip, path, json, cancellation);
        }

        private static async Task WriteJsonAsync(ZipArchive zip, string path, string content, CancellationToken cancellation)
        {
            ZipArchiveEntry entry = zip.CreateEntry(path, CompressionLevel.Optimal);
            await using Stream stream = entry.Open();
            byte[] bytes = Encoding.UTF8.GetBytes(content);
            await stream.WriteAsync(bytes, cancellation);
        }

        private static string Csv(string? value)
        {
            string v = value ?? string.Empty;
            return v.Contains(',') || v.Contains('"') ? $"\"{v.Replace("\"", "\"\"")}\"" : v;
        }
    }
}
