using Microsoft.Extensions.DependencyInjection;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Agent.Commands;
using PlayGround.Application.Claim.Commands;
using PlayGround.Application.Landing.Commands;
using PlayGround.Application.Notification.Commands;
using PlayGround.Application.Player.Commands;
using PlayGround.Application.Records.Commands;
using PlayGround.Application.Team.Commands;
using PlayGround.Application.Export.Commands;
using PlayGround.Persistence;
using PlayGround.Server.Services;

namespace PlayGround.Server.DependencyInjection
{
    /// <summary>축구 도메인: 저장소 + 유즈케이스(랜딩·선수·팀). 종목별로 이런 모듈을 하나씩 둔다.</summary>
    public static class SoccerModule
    {
        public static IServiceCollection AddSoccerServices(this IServiceCollection services)
        {
            services.AddSoccerPersistence();
            services.AddScoped<SoccerLandingContentsCommand>();
            services.AddScoped<SoccerPlayerProfileCommand>();
            services.AddScoped<SoccerPlayerInfoCommand>();
            services.AddScoped<SoccerManagedPlayersCommand>();
            services.AddScoped<SoccerPlayerFieldVisibilityCommand>();
            services.AddScoped<SoccerPlayerProfileInfoUpdateCommand>();
            services.AddScoped<SoccerPlayerPhotoCommand>();
            services.AddScoped<SoccerPlayerClaimCommand>();
            services.AddScoped<SoccerPlayerCareerCommand>();
            services.AddScoped<SoccerPlayerCareerSaveCommand>();
            services.AddScoped<SoccerPlayerPortfolioSaveCommand>();
            services.AddScoped<SoccerPlayerPortfolioCommand>();
            services.AddScoped<SoccerPlayerSeasonStatsCommand>();
            services.AddScoped<SoccerPlayerPublicProfileCommand>();
            services.AddScoped<SoccerPlayerStrengthTagsCommand>();
            services.AddScoped<SoccerTeamCommand>();
            services.AddScoped<SoccerTeamInfoCommand>();
            services.AddScoped<SoccerTeamRosterCommand>();
            services.AddScoped<SoccerTeamPublicHomeCommand>();
            services.AddScoped<SoccerTeamExploreCommand>();
            services.AddScoped<SoccerTeamSeasonRecordCommand>();
            services.AddScoped<SoccerTeamMatchesCommand>();
            services.AddScoped<SoccerTeamMatchResultCommand>();
            services.AddScoped<SoccerTeamInfoUpdateCommand>();
            services.AddScoped<SoccerTeamRecruitmentCommand>();
            services.AddScoped<SoccerTeamPostCommand>();
            services.AddScoped<SoccerApplicationCommand>();
            services.AddScoped<SoccerScheduleCommand>();
            services.AddScoped<SoccerTeamCareerOutcomeCommand>();
            services.AddScoped<SoccerTeamReviewCommand>();
            services.AddScoped<SoccerRecordCorrectionCommand>();
            services.AddScoped<SoccerTeamRosterWriteCommand>();
            services.AddScoped<SoccerActionItemsCommand>();
            services.AddScoped<SoccerDashboardHubCommand>();
            services.AddScoped<SoccerClaimFlowCommand>();
            services.AddScoped<SoccerClaimReviewCommand>();
            services.AddScoped<SoccerNotificationCommand>();
            services.AddScoped<SoccerAgentApprovalCommand>();

            // 업로드 이미지 저장 — 지금은 로컬 디스크, 오브젝트 스토리지로 갈 때 이 줄만 바꾼다
            services.AddSingleton<IImageStorage, LocalImageStorageService>();
            // 첨부 문서 저장(게시판 pdf·hwp 등) — 원본 확장자 보존. 이미지 저장과 같은 로컬 디스크 어댑터.
            services.AddSingleton<IFileStorage, LocalFileStorageService>();

            // 링크 공유 미리보기(OG 메타, DECISION.OGMETA) — 크롤러 감지 미들웨어 + 카드 렌더 + 24h 캐시
            services.AddMemoryCache();
            services.AddScoped<OgMetaService>();
            services.AddSingleton<OgImageRenderer>();

            //.// 데이터 내려받기 (Design.SettingsFlows ③) — 요청 접수 + 백그라운드 잡 + 서명 URL 다운로드
            services.AddScoped<DataExportCommand>();
            services.AddSingleton<IEmailSender, LogOnlyEmailSender>();     // 발송 인프라 생기면 어댑터 교체
            services.AddSingleton<IExportStorage, LocalExportStorage>();   // 비공개 경로(정적 서빙 밖)
            services.AddSingleton<IDataExportQueue, DataExportQueue>();    // 인메모리 큐(워커와 공유)
            services.AddHostedService<DataExportWorker>();                 // 백그라운드 생성 워커
            services.AddScoped<SoccerTeamVideosCommand>();
            services.AddScoped<SoccerRecordsTournamentsCommand>();
            services.AddScoped<SoccerRecordsTournamentDetailCommand>();
            services.AddScoped<SoccerRecordsMatchDetailCommand>();
            return services;
        }
    }
}
