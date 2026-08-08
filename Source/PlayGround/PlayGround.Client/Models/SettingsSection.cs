using PlayGround.Client.Localization;

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
        public static string ToSlug(this SettingsSection section)
        {
            return section.ToString().ToLowerInvariant();
        }

        public static string ToLabel(this SettingsSection section)
        {
            return section switch
            {
                SettingsSection.Roles => AppText.Enums.SettingsRoles,
                SettingsSection.Notifications => AppText.Enums.SettingsNotifications,
                _ => AppText.Enums.SettingsAccount,
            };
        }

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
