using Microsoft.JSInterop;

namespace PlayGround.Client.Localization
{
    /// <summary>활성 문화권 상태 + 전환 + localStorage 지속. 전환 시 Changed 발생 → 구독 컴포넌트 재렌더.
    /// 지원 문화권은 추후 ja/en 추가 시 확장(현재 ko 기본).</summary>
    public sealed class CultureState
    {
        public const string StorageKey = "pg.culture";

        private readonly JsonLocalizer mLocalizer;
        private readonly IJSRuntime mJs;

        public string Culture => mLocalizer.Culture;

        public event Action? Changed;

        public CultureState(JsonLocalizer localizer, IJSRuntime js)
        {
            mLocalizer = localizer;
            mJs = js;
        }

        public async Task SetCultureAsync(string culture)
        {
            if (culture == mLocalizer.Culture)
            {
                return;
            }

            await mLocalizer.LoadAsync(culture);
            try
            {
                await mJs.InvokeVoidAsync("localStorage.setItem", StorageKey, culture);
            }
            catch (JSException)
            {
            }

            Changed?.Invoke();
        }

        public async Task<string?> ReadStoredAsync()
        {
            try
            {
                return await mJs.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            }
            catch (JSException)
            {
                return null;
            }
        }
    }
}
