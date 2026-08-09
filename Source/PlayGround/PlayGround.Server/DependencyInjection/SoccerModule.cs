using PlayGround.Application.Agent.Commands;
using PlayGround.Application.Claim.Commands;
using PlayGround.Application.Export.Commands;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Landing.Commands;
using PlayGround.Application.Notification.Commands;
using PlayGround.Application.Player.Commands;
using PlayGround.Application.Records.Commands;
using PlayGround.Application.Team.Commands;
using PlayGround.Infrastructure.Logging;
using PlayGround.Persistence;
using PlayGround.Server.Services;

namespace PlayGround.Server.DependencyInjection
{
    /// <summary>축구 도메인: 저장소 + 유즈케이스(랜딩·선수·팀). 종목별로 이런 모듈을 하나씩 둔다.</summary>
    public static class SoccerModule
    {
        private static readonly NLog.ILogger Logger = NLog.LogManager.GetCurrentClassLogger();

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

            //.// 업로드 저장 — Provider가 어댑터를 고른다 (DatabaseConfiguration과 같은 방식).
            // URL은 어느 Provider든 "/uploads/..." — DB 저장값·클라이언트·검증 로직이 저장 위치를 모르게 유지한다.
            // 소비자(Remote*)는 IObjectStore만 보므로, 벤더를 아는 곳은 어댑터 한 개뿐이다.

            UploadStorageConfiguration uploadConfig =
                configuration.GetSection(UploadStorageConfiguration.Section).Get<UploadStorageConfiguration>()
                ?? new UploadStorageConfiguration();
            Logger.InfoWith("Upload storage configured",
                ("Provider", uploadConfig.Provider),
                ("Bucket", uploadConfig.UsesLocalDisk ? null : uploadConfig.BucketName),
                ("Region", uploadConfig.UsesLocalDisk ? null : uploadConfig.Region));

            if (uploadConfig.UsesLocalDisk)
            {
                // **운영에서 안 쓰더라도 로컬 디스크 어댑터는 남겨 둔다** — 클라우드 자격 증명이 없는 PC나
                // 오프라인에서 앱을 띄우는 경로이고, 오브젝트 스토리지 장애 시의 대피로이기도 하다.
                // 지우면 Provider=Local이 죽는다(= appsettings 한 줄로 되돌릴 수단이 사라진다).
                services.AddSingleton<IImageStorage, LocalImageStorageService>();
                services.AddSingleton<IFileStorage, LocalFileStorageService>();
                services.AddSingleton<IUploadReader, LocalUploadReader>();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(uploadConfig.BucketName))
                {
                    // 설정 누락을 조용히 로컬 폴백하면 운영에서 이미지가 인스턴스와 함께 사라진다 — 기동 실패가 낫다
                    throw new InvalidOperationException(
                        $"UploadStorageConfiguration: Provider={uploadConfig.Provider} requires BucketName");
                }

                services.AddSingleton<IObjectStore>(_ => CreateObjectStore(uploadConfig));
                services.AddSingleton<IImageStorage, RemoteImageStorageService>();
                services.AddSingleton<IFileStorage, RemoteFileStorageService>();
                services.AddSingleton<IUploadReader, RemoteUploadReader>();
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

            // 셋 다 아직 대체물이다. 기동 로그에 남지 않으면 "메일이 안 온다"를 코드를 열어야 안다.
            Logger.InfoWith("Data export configured",
                ("EmailSender", nameof(LogOnlyEmailSender)),
                ("EmailDelivery", "None"),
                ("Storage", nameof(LocalExportStorage)),
                ("Queue", "InMemory"));
            services.AddScoped<SoccerTeamVideosCommand>();
            services.AddScoped<SoccerRecordsTournamentsCommand>();
            services.AddScoped<SoccerRecordsTournamentDetailCommand>();
            services.AddScoped<SoccerRecordsMatchDetailCommand>();

#if DEBUG
            //.// 시간 이동 — DB의 SystemClockOffset을 앱 시계(DebugClock)에 맞춘다.
            // RELEASE 빌드에는 이 등록도 서비스도 컴파일되지 않는다.
            services.AddSingleton<DebugClockRepository>();
            services.AddHostedService<DebugClockSyncService>();
#endif

            return services;
        }

        // Provider 값 하나가 어댑터 하나에 대응한다 — 새 저장소를 붙이면 여기에 한 줄이 는다.
        // Google·Azure는 껍질만 있고 생성자가 기동 시점에 실패한다(업로드 때 알게 되면 늦다).
        private static IObjectStore CreateObjectStore(UploadStorageConfiguration config) => config.Provider switch
        {
            UploadStorageProvider.Aws => new AwsObjectStore(config),
            UploadStorageProvider.Google => new GoogleObjectStore(config),
            UploadStorageProvider.Azure => new AzureObjectStore(config),
            _ => throw new InvalidOperationException(
                $"UploadStorageConfiguration: unsupported Provider '{config.Provider}'"),
        };
    }
}
