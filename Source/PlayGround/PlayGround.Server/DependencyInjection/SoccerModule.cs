using Microsoft.Extensions.Configuration;
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
        public static IServiceCollection AddSoccerServices(this IServiceCollection services, IConfiguration configuration)
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

            //.// 업로드 저장 — Provider 스위치 (Local=디스크+정적 서빙 / Remote=오브젝트 스토리지+/uploads 프록시).
            // URL은 두 백엔드 모두 "/uploads/..." — DB 저장값·클라이언트·검증 로직이 저장 위치를 모르게 유지한다.
            // **저장소 벤더를 아는 곳은 AwsObjectStore 하나뿐이다** — 여기서도 IObjectStore로만 등록한다.

            UploadStorageConfiguration uploadConfig =
                configuration.GetSection(UploadStorageConfiguration.Section).Get<UploadStorageConfiguration>()
                ?? new UploadStorageConfiguration();
            if (uploadConfig.UsesRemote)
            {
                if (string.IsNullOrWhiteSpace(uploadConfig.Remote.BucketName))
                {
                    // 설정 누락을 조용히 로컬 폴백하면 운영에서 이미지가 인스턴스와 함께 사라진다 — 기동 실패가 낫다
                    throw new InvalidOperationException(
                        "UploadStorageConfiguration: Provider=Remote requires Remote.BucketName");
                }

                services.AddSingleton<IObjectStore>(_ => new AwsObjectStore(uploadConfig.Remote));
                services.AddSingleton<IImageStorage, RemoteImageStorageService>();
                services.AddSingleton<IFileStorage, RemoteFileStorageService>();
                services.AddSingleton<IUploadReader, RemoteUploadReader>();
            }
            else
            {
                // **운영에서 안 쓰더라도 로컬 디스크 어댑터는 남겨 둔다** — AWS 자격 증명이 없는 PC나
                // 오프라인에서 앱을 띄우는 경로이고, 오브젝트 스토리지 장애 시의 대피로이기도 하다.
                // 지우면 Provider=Local이 죽는다(= appsettings 한 줄로 되돌릴 수단이 사라진다).
                services.AddSingleton<IImageStorage, LocalImageStorageService>();
                services.AddSingleton<IFileStorage, LocalFileStorageService>();
                services.AddSingleton<IUploadReader, LocalUploadReader>();
            }

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
