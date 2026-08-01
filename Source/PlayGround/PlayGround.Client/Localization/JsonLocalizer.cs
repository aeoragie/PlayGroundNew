using System.Net.Http.Json;

namespace PlayGround.Client.Localization
{
    /// <summary>wwwroot/i18n/{Domain}.{culture}.json 지연 로드 + 병합. 활성 문화권만 fetch(WASM 다운로드 최소),
    /// 기본 문화권(ko)은 폴백으로 항상 유지. 단일 인스턴스(WASM). 도메인 목록은 생성된 AppText.Domains.</summary>
    public sealed class JsonLocalizer : ILocalizer
    {
        public const string BaseCulture = "ko";

        private readonly HttpClient mHttp;
        private Dictionary<string, string> mBase = new(StringComparer.Ordinal);   // ko 폴백
        private Dictionary<string, string> mActive = new(StringComparer.Ordinal); // 현재 문화권

        public string Culture { get; private set; } = BaseCulture;

        public JsonLocalizer(HttpClient http)
        {
            mHttp = http;
        }

        public string Get(string key)
        {
            if (mActive.TryGetValue(key, out string? value))
            {
                return value;
            }

            if (mBase.TryGetValue(key, out string? baseValue))
            {
                return baseValue;
            }

            return key; // 최후 폴백 — 키 그대로 노출(번역 누락 감지용)
        }

        public string Format(string key, params object[] args)
        {
            try
            {
                return string.Format(Get(key), args);
            }
            catch (FormatException)
            {
                return Get(key);
            }
        }

        /// <summary>기본 문화권을 폴백으로 확보하고, 활성 문화권을 로드/교체한다.</summary>
        public async Task LoadAsync(string culture)
        {
            if (mBase.Count == 0)
            {
                mBase = await FetchCultureAsync(BaseCulture);
            }

            mActive = culture == BaseCulture ? mBase : await FetchCultureAsync(culture);
            Culture = culture;
        }

        private async Task<Dictionary<string, string>> FetchCultureAsync(string culture)
        {
            var merged = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (string domain in AppText.Domains)
            {
                try
                {
                    Dictionary<string, string>? file =
                        await mHttp.GetFromJsonAsync<Dictionary<string, string>>($"i18n/{domain}.{culture}.json");
                    if (file is null)
                    {
                        continue;
                    }

                    foreach ((string localKey, string value) in file)
                    {
                        merged[$"{domain}.{localKey}"] = value;
                    }
                }
                catch
                {
                    // 해당 문화권의 도메인 파일 없음 → 기본 문화권 폴백에 맡긴다
                }
            }

            return merged;
        }
    }
}
