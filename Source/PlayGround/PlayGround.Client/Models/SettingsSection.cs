namespace PlayGround.Client.Models
{
    /// <summary>설정 화면 탭. 라우트 슬러그는 소문자 이름 (/settings/{slug}).</summary>
    public enum SettingsSection
    {
        Account,
        Roles,
        Notifications,
    }

    public static class SettingsSectionExtensions
    {
        /// <summary>라우트 슬러그로 변환 (예: Roles → "roles").</summary>
        public static string ToSlug(this SettingsSection section)
        {
            return section.ToString().ToLowerInvariant();
        }

        /// <summary>메뉴·탭 라벨 (Design.Settings 카피 고정).</summary>
        public static string ToLabel(this SettingsSection section)
        {
            return section switch
            {
                SettingsSection.Roles => "역할",
                SettingsSection.Notifications => "알림",
                _ => "계정",
            };
        }

        /// <summary>라우트 슬러그를 섹션으로 해석. 미지정·미지원 슬러그는 Account.</summary>
        public static SettingsSection ParseSlug(string? slug)
        {
            // Enum.TryParse는 숫자 문자열("1")도 통과시키므로 이름 형태만 허용한다.
            if (!string.IsNullOrEmpty(slug)
                && !char.IsAsciiDigit(slug[0])
                && Enum.TryParse(slug, ignoreCase: true, out SettingsSection section))
            {
                return section;
            }

            return SettingsSection.Account;
        }
    }
}
