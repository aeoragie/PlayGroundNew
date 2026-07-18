namespace PlayGround.Client.Components.Shared.Forms
{
    /// <summary>셀렉트 옵션 한 개. Description은 바텀시트에서 부가정보로 노출된다
    /// (네이티브 셀렉트를 쓰지 않는 이유 — Design.FormPatterns).</summary>
    /// <param name="Value">선택 값</param>
    /// <param name="Label">표시 문구</param>
    /// <param name="Description">부가 설명 (선택)</param>
    public sealed record SelectOption<TValue>(TValue Value, string Label, string? Description = null);
}
