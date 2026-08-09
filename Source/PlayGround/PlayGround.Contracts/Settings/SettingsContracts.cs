using PlayGround.Shared.Time;

namespace PlayGround.Contracts.Settings
{
    public class AccountSettingsResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public string MaskedEmail { get; set; } = string.Empty;

        public string AuthProvider { get; set; } = string.Empty;
        public List<LinkedLoginDto> SocialLogins { get; set; } = new();

        public int NameChangeRemaining { get; set; } = 2;

        public SystemTime? NameChangeAvailableAt { get; set; }

        public int LoginMeansCount { get; set; }
    }

    public class LinkedLoginDto
    {
        public string Provider { get; set; } = string.Empty;
        public SystemTime LinkedAt { get; set; }

        public string? MaskedEmail { get; set; }
    }

    public class ChangeDisplayNameRequest
    {
        public string DisplayName { get; set; } = string.Empty;
    }

    public class NotificationPreferencesResponse
    {
        public List<NotificationPreferenceDto> Preferences { get; set; } = new();
    }

    public class NotificationPreferenceDto
    {
        public string ItemName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    public class SetNotificationPreferenceRequest
    {
        public string ItemName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
