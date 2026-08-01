namespace PlayGround.Client.Localization
{
    /// <summary>키 → 현재 문화권 문자열 해석 (미존재 시 기본 문화권 → 키 순 폴백).
    /// 생성된 타입드 접근자(AppText.*)가 소비한다. 직접 문자열 키 호출은 지양.</summary>
    public interface ILocalizer
    {
        string Get(string key);

        string Format(string key, params object[] args);
    }
}
