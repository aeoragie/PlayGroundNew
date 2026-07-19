using Microsoft.JSInterop;

namespace PlayGround.Client.Services.Auth
{
    /// <summary>
    /// 로그인 왕복용 returnUrl 보관소 (Design.Navigation 인증 플로우 1).
    /// sessionStorage에 두는 이유: 소셜 로그인은 외부 도메인을 거쳐 전체 페이지 리로드로 돌아오므로
    /// 메모리 상태가 날아간다. 탭을 닫으면 함께 사라져 localStorage보다 수명이 적절하다.
    /// </summary>
    public sealed class ReturnUrlStore
    {
        private const string StorageKey = "pg.returnUrl";

        private readonly IJSRuntime mJs;

        public ReturnUrlStore(IJSRuntime js)
        {
            ArgumentNullException.ThrowIfNull(js);
            mJs = js;
        }

        /// <summary>로그인 진입 시 저장. 안전하지 않은 값은 저장하지 않는다.</summary>
        public async Task SaveAsync(string? returnUrl)
        {
            if (!Routes.IsSafeReturnUrl(returnUrl))
            {
                await ClearAsync();
                return;
            }

            await mJs.InvokeVoidAsync("sessionStorage.setItem", StorageKey, returnUrl);
        }

        /// <summary>인증 완료 지점에서 1회 소비 — 읽는 즉시 지운다.</summary>
        public async Task<string?> ConsumeAsync()
        {
            string? saved = await mJs.InvokeAsync<string?>("sessionStorage.getItem", StorageKey);
            await ClearAsync();
            return Routes.IsSafeReturnUrl(saved) ? saved : null;
        }

        public async Task ClearAsync()
        {
            await mJs.InvokeVoidAsync("sessionStorage.removeItem", StorageKey);
        }
    }
}
