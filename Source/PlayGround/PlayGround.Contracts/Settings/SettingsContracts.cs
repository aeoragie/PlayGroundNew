using System;
using System.Collections.Generic;

namespace PlayGround.Contracts.Settings
{
    /// <summary>계정 설정 묶음 (설정 · 계정 탭). 이메일은 마스킹된 값만 내려간다 (kim***@gmail.com).</summary>
    public class AccountSettingsResponse
    {
        public string DisplayName { get; set; } = string.Empty;
        public string MaskedEmail { get; set; } = string.Empty;

        /// <summary>기본 로그인 수단 — 'Local'(이메일) | 'Google' | 'Kakao'.</summary>
        public string AuthProvider { get; set; } = string.Empty;
        public List<LinkedLoginDto> SocialLogins { get; set; } = new();
    }

    /// <summary>연결된 소셜 로그인 한 개.</summary>
    public class LinkedLoginDto
    {
        public string Provider { get; set; } = string.Empty; // 'Google' | 'Kakao'
        public DateTime LinkedAt { get; set; }
    }

    /// <summary>알림 설정 — 6개 항목 전부 포함 (저장값 없는 항목은 기본값). 승인형은 목록에 없다(항상 켜짐).</summary>
    public class NotificationPreferencesResponse
    {
        public List<NotificationPreferenceDto> Preferences { get; set; } = new();
    }

    /// <summary>알림 설정 한 항목. ItemName은 NotificationPreferenceItem enum 멤버 이름 문자열.</summary>
    public class NotificationPreferenceDto
    {
        public string ItemName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }

    /// <summary>알림 설정 변경 요청 — 승인형 항목은 서버가 거부한다.</summary>
    public class SetNotificationPreferenceRequest
    {
        public string ItemName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; }
    }
}
