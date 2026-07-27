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

        /// <summary>남은 이름 변경 횟수 (30일 창 기준, 0~2). 0이면 버튼 비활성 + "다음 변경 가능" 캡션.</summary>
        public int NameChangeRemaining { get; set; } = 2;

        /// <summary>다음 이름 변경 가능 시각 — 제한 초과(Remaining=0)일 때만 값. 가장 오래된 최근 변경 + 30일.</summary>
        public DateTime? NameChangeAvailableAt { get; set; }

        /// <summary>로그인 수단 총 개수 (소셜 + 비밀번호). 1이면 그 수단은 해제 불가 → "유일한 로그인 수단" 캡션.</summary>
        public int LoginMeansCount { get; set; }
    }

    /// <summary>연결된 소셜 로그인 한 개.</summary>
    public class LinkedLoginDto
    {
        public string Provider { get; set; } = string.Empty; // 'Google' | 'Kakao'
        public DateTime LinkedAt { get; set; }

        /// <summary>연결된 소셜 계정 이메일 — 항상 마스킹 (kim***@gmail.com). 없으면 null.</summary>
        public string? MaskedEmail { get; set; }
    }

    /// <summary>이름 변경 요청 (Design.SettingsFlows ①). 검증은 서버·클라 동일 규칙.</summary>
    public class ChangeDisplayNameRequest
    {
        public string DisplayName { get; set; } = string.Empty;
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
