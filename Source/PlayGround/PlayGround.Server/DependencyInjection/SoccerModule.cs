using Amazon;
using Amazon.S3;
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

            //.// 업로드 저장 — Provider 스위치 (Local=디스크+정적 서빙 / S3=프라이빗 버킷+/uploads 프록시).
            // URL은 두 백엔드 모두 "/uploads/..." — DB 저장값·클라이언트·검증 로직이 저장 위치를 모르게 유지한다.

            UploadStorageConfiguration uploadConfig =
                configuration.GetSection(UploadStorageConfiguration.Section).Get<UploadStorageConfiguration>()
                ?? new UploadStorageConfiguration();
            if (uploadConfig.UsesS3)
            {
                if (string.IsNullOrWhiteSpace(uploadConfig.S3.BucketName))
                {
                    // 설정 누락을 조용히 로컬 폴백하면 운영에서 이미지가 인스턴스와 함께 사라진다 — 기동 실패가 낫다
                    throw new InvalidOperationException(
                        "UploadStorageConfiguration: Provider=S3 requires S3.BucketName");
                }

                services.AddSingleton(uploadConfig.S3);
                services.AddSingleton<IAmazonS3>(_ => CreateS3Client(uploadConfig.S3));
                services.AddSingleton<IImageStorage, S3ImageStorageService>();
                services.AddSingleton<IFileStorage, S3FileStorageService>();
                services.AddSingleton<IUploadReader, S3UploadReader>();
            }
            else
            {
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

        // 자격 증명은 SDK 기본 체인(EC2 인스턴스 역할·로컬 프로필) — 서버에 액세스 키를 두지 않는다
        private static IAmazonS3 CreateS3Client(UploadStorageConfiguration.S3Settings settings)
        {
            return string.IsNullOrEmpty(settings.Region)
                ? new AmazonS3Client()
                : new AmazonS3Client(RegionEndpoint.GetBySystemName(settings.Region));
        }
    }
}
