using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace PlayGround.Client.Components.Shared.Forms
{
    /// <summary>폼 스크롤·포커스 JS interop. 모듈을 한 번만 import해 필드들이 공유한다.</summary>
    public static class FormScrollInterop
    {
        private const string ModulePath = "./js/forms.js";

        private static IJSObjectReference? mModule;

        /// <summary>첫 오류 필드로 스크롤 + 포커스 (모바일은 하단 고정 바 높이만큼 오프셋).</summary>
        public static async Task ScrollIntoViewAndFocusAsync(IJSRuntime js, ElementReference element)
        {
            mModule ??= await js.InvokeAsync<IJSObjectReference>("import", ModulePath);
            await mModule.InvokeVoidAsync("scrollIntoViewAndFocus", element);
        }
    }
}
