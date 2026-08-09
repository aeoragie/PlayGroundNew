using FluentAssertions;
using PlayGround.Domain.Account;
using PlayGround.Domain.Soccer;
using Xunit;

namespace PlayGround.Tests.Unit.Domain
{
    /// <summary>Domain 열거형의 **규칙**(파싱 가드·기본값)을 고정한다. 표시 라벨은 여기 없다 —
    /// 표현 계층(Client의 SoccerDomainEnumLabels)이 리소스로 소유한다.</summary>
    public class SoccerEnumRulesTests
    {
        //.// 수정 신청 항목 — 숫자 문자열 파싱 차단

        [Theory]
        [InlineData("Score", SoccerCorrectionField.Score)]
        [InlineData("GoalAssist", SoccerCorrectionField.GoalAssist)]
        [InlineData("Appearance", SoccerCorrectionField.Appearance)]
        [InlineData("Other", SoccerCorrectionField.Other)]
        public void CorrectionField_ParsesMemberNames(string value, SoccerCorrectionField expected)
        {
            SoccerCorrectionFieldExtensions.TryParse(value, out SoccerCorrectionField field).Should().BeTrue();
            field.Should().Be(expected);
        }

        [Theory]
        [InlineData("0")]   // Enum.TryParse는 숫자를 그대로 받아들인다 — 의도적으로 막는다
        [InlineData("1")]
        [InlineData("99")]
        [InlineData("Unknown")]
        [InlineData("")]
        [InlineData(null)]
        public void CorrectionField_RejectsNumericAndUnknown_FallsBackToOther(string? value)
        {
            SoccerCorrectionFieldExtensions.TryParse(value, out SoccerCorrectionField field).Should().BeFalse();
            field.Should().Be(SoccerCorrectionField.Other);
        }

        //.// 수정 신청 상태 — 알 수 없으면 접수(Pending)

        [Theory]
        [InlineData("Pending", SoccerCorrectionStatus.Pending)]
        [InlineData("Accepted", SoccerCorrectionStatus.Accepted)]
        [InlineData("Rejected", SoccerCorrectionStatus.Rejected)]
        [InlineData("Whatever", SoccerCorrectionStatus.Pending)]
        [InlineData(null, SoccerCorrectionStatus.Pending)]
        public void CorrectionStatus_FallsBackToPending_WhenParseFails(string? value, SoccerCorrectionStatus expected)
        {
            SoccerCorrectionStatusExtensions.Parse(value).Should().Be(expected);
        }

        //.// 처리 필요 항목 — 알 수 없으면 연결(Invite)

        [Theory]
        [InlineData("Invite", SoccerActionKind.Invite)]
        [InlineData("Correction", SoccerActionKind.Correction)]
        [InlineData("Access", SoccerActionKind.Invite)]  // 에이전트 축 도입 전 — 기본값으로 떨어진다
        [InlineData(null, SoccerActionKind.Invite)]
        public void ActionKind_FallsBackToInvite_WhenParseFails(string? value, SoccerActionKind expected)
        {
            SoccerActionKindExtensions.Parse(value).Should().Be(expected);
        }

        //.// 프로필 공개 기본값 — SPEC.PLAYERDASHBOARD §1

        [Theory]
        [InlineData(SoccerPlayerProfileField.Profile, true)]
        [InlineData(SoccerPlayerProfileField.Height, true)]
        [InlineData(SoccerPlayerProfileField.Weight, true)]
        [InlineData(SoccerPlayerProfileField.PreferredFoot, true)]
        [InlineData(SoccerPlayerProfileField.StrengthTags, true)]
        [InlineData(SoccerPlayerProfileField.School, false)]         // 미성년자 보호 — 기본 비공개
        [InlineData(SoccerPlayerProfileField.GuardianPhone, false)]  // 연락처 — 기본 비공개
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
            names.Should().HaveCount(6);
        }
    }
}
