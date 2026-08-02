using System.Text.Json;
using Xunit;
using PlayGround.Client.Localization;

namespace PlayGround.Tests.Unit.Client
{
    /// <summary>디스크의 `wwwroot/i18n` 리소스를 실제 로더로 읽어 AppText에 물린다.
    /// 이걸 안 하면 AppText가 NullLocalizer라 키 문자열이 그대로 나와, 표시 로직 테스트가
    /// 카피를 전혀 검증하지 못한다. 컬렉션 픽스처라 스위트당 한 번만 로드한다.</summary>
    public sealed class LocalizationFixture : IAsyncLifetime
    {
        public string ResourceDirectory { get; } = FindResourceDirectory();

        public JsonLocalizer Localizer { get; private set; } = null!;

        public async ValueTask InitializeAsync()
        {
            Localizer = new JsonLocalizer(new HttpClient(new DiskHandler(ResourceDirectory))
            {
                BaseAddress = new Uri("http://localhost/"),
            });

            await Localizer.LoadAsync(JsonLocalizer.BaseCulture);
            AppText.Loc = Localizer;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public Dictionary<string, string> Load(string domain, string culture)
        {
            string path = Path.Combine(ResourceDirectory, $"{domain}.{culture}.json");
            return JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path))!;
        }

        /// <summary>테스트는 Binary/ 아래에서 도므로 리포지토리 루트를 거슬러 올라가 찾는다.</summary>
        private static string FindResourceDirectory()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir is not null)
            {
                string candidate = Path.Combine(dir.FullName,
                    "Source", "PlayGround", "PlayGround.Client", "wwwroot", "i18n");
                if (Directory.Exists(candidate))
                {
                    return candidate;
                }

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException("wwwroot/i18n 을 찾지 못했다");
        }

        /// <summary>`i18n/{Domain}.{culture}.json` 요청을 디스크에서 돌려준다 — 로더 자체도 함께 검증된다.</summary>
        private sealed class DiskHandler(string directory) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                string path = Path.Combine(directory, Path.GetFileName(request.RequestUri!.AbsolutePath));
                if (!File.Exists(path))
                {
                    return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NotFound));
                }

                return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText(path), System.Text.Encoding.UTF8, "application/json"),
                });
            }
        }
    }

    /// <summary>AppText.Loc은 정적 상태라 병렬 실행이 겹치면 안 된다 — 한 컬렉션으로 묶어 직렬화한다.</summary>
    [CollectionDefinition(Name)]
    public sealed class LocalizationCollection : ICollectionFixture<LocalizationFixture>
    {
        public const string Name = "Localization";
    }
}
