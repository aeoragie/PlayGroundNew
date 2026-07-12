using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace PlayGround.Client.Services.Auth
{
    /// <summary>액세스 토큰을 브라우저 localStorage에 보관·조회·삭제한다.</summary>
    public sealed class TokenStore
    {
        private const string TokenKey = "pg.accessToken";

        private readonly IJSRuntime mJs;

        public TokenStore(IJSRuntime js)
        {
            ArgumentNullException.ThrowIfNull(js);
            Debug.Assert(js is not null);
            mJs = js;
        }

        public async Task<string?> GetTokenAsync()
        {
            return await mJs.InvokeAsync<string?>("localStorage.getItem", TokenKey);
        }

        public async Task SaveTokenAsync(string token)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(token);
            Debug.Assert(!string.IsNullOrWhiteSpace(token));
            await mJs.InvokeVoidAsync("localStorage.setItem", TokenKey, token);
        }

        public async Task ClearTokenAsync()
        {
            await mJs.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        }
    }
}
