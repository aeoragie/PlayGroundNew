using FluentAssertions;
using PlayGround.Domain.Account;
using PlayGround.Domain.Soccer;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>열거형의 **규칙**(어휘·기본값)을 고정한다. 문자열 파싱 가드는 경계가 소유한다 —
    /// 와이어는 LenientEnumJsonConverterTests, DB는 EnumColumn. 표시 라벨은 Client의 SoccerDomainEnumLabels.</summary>
    public class SoccerEnumRulesTests
    {
        //.// 프로필 공개 기본값 — SPEC.PLAYERDASHBOARD §1

        [Theory]
        [InlineData(SoccerPlayerProfileField.Profile, true)]
        [InlineData(SoccerPlayerProfileField.Height, true)]
        [InlineData(SoccerPlayerProfileField.Weight, true)]
        [InlineData(SoccerPlayerProfileField.PreferredFoot, true)]
        [InlineData(SoccerPlayerProfileField.StrengthTags, true)]
        [InlineData(SoccerPlayerProfileField.School, false)]         // 미성년자 보호 — 기본 비공개
        [InlineData(SoccerPlayerProfileField.GuardianPhone, false)]  // 연락처 — 기본 비공개
        [InlineData(SoccerPlayerProfileField.Unknown, false)]
        public void ProfileField_DefaultVisibility_MatchesSpec(SoccerPlayerProfileField field, bool expected)
        {
            field.DefaultIsPublic().Should().Be(expected);
        }

        //.// 알림 설정 기본값 — Design.Settings

        [Theory]
        [InlineData(NotificationPreferenceItem.PushChannel, true)]
        [InlineData(NotificationPreferenceItem.MatchResult, true)]
        [InlineData(NotificationPreferenceItem.Recruit, true)]
        [InlineData(NotificationPreferenceItem.Review, true)]
        [InlineData(NotificationPreferenceItem.EmailChannel, false)]
        [InlineData(NotificationPreferenceItem.VisitSummary, false)]
        [InlineData(NotificationPreferenceItem.Unknown, false)]
        public void NotificationPreference_Defaults_MatchSpec(NotificationPreferenceItem item, bool expected)
        {
            item.DefaultIsEnabled().Should().Be(expected);
        }

        [Fact]
        public void NotificationPreference_ApprovalTypes_AreNotInEnum()
        {
            // 미성년자 보호 관문이라 항상 켜짐 — enum이 저장 화이트리스트이므로
            // 여기에 항목이 추가되면 클라이언트가 끌 수 있게 되어 버린다.
            string[] names = Enum.GetNames<NotificationPreferenceItem>();
            names.Should().NotContain("ClaimRequest");
            names.Should().NotContain("ViewRequest");
            names.Should().HaveCount(7);   // Unknown + 항목 6종
        }

        [Fact]
        public void WireEnums_HaveUnknownAsDefault()
        {
            // 비널러블 정책의 전제 — default(enum)이 곧 "미지정"이어야 한다.
            default(SoccerCorrectionField).Should().Be(SoccerCorrectionField.Unknown);
            default(SoccerCorrectionStatus).Should().Be(SoccerCorrectionStatus.Unknown);
            default(SoccerActionKind).Should().Be(SoccerActionKind.Unknown);
            default(SoccerPlayerProfileField).Should().Be(SoccerPlayerProfileField.Unknown);
            default(NotificationPreferenceItem).Should().Be(NotificationPreferenceItem.Unknown);
            default(AccountRole).Should().Be(AccountRole.Unknown);
        }
    }
}
