using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Settings;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Commands;
using PlayGround.Application.Settings.Commands;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>저장 화이트리스트 가드 2종 — 알림 설정 항목과 프로필 공개 항목.
    /// 둘 다 enum이 화이트리스트라서 **클라이언트가 우회해도 서버에서 끝나야** 한다.</summary>
    public class SettingsGuardCommandTests
    {
        private static readonly Guid User = Guid.NewGuid();

        //.// 알림 설정 — 승인형은 저장 자체가 거부돼야 한다

        private static NotificationPreferenceCommand PreferenceCommand(bool saved = true)
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(r => r.SetNotificationPreferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(saved));
            return new NotificationPreferenceCommand(repo.Object, NullLogger<NotificationPreferenceCommand>.Instance);
        }

        [Theory]
        [InlineData("PushChannel")]
        [InlineData("EmailChannel")]
        [InlineData("MatchResult")]
        [InlineData("Recruit")]
        [InlineData("Review")]
        [InlineData("VisitSummary")]
        public async Task SetAsync_SavesAllowedKeys(string itemName)
        {
            Result<bool> result = await PreferenceCommand().SetAsync(User,
                new SetNotificationPreferenceRequest { ItemName = itemName, IsEnabled = false });

            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("ClaimRequest")]    // 승인형 — 미성년자 보호 관문이라 끌 수 없다
        [InlineData("ViewRequest")]
        [InlineData("pushchannel")]     // 대소문자가 다르면 거부 (정확히 일치해야 한다)
        [InlineData("PUSHCHANNEL")]
        [InlineData("0")]               // 숫자 문자열이 enum으로 파싱되는 것을 막는다
        [InlineData("2")]
        [InlineData("")]
        [InlineData("Unknown")]
        public async Task SetAsync_ApprovalAndUnknownKeys_AreInvalidInput(string itemName)
        {
            Result<bool> result = await PreferenceCommand().SetAsync(User,
                new SetNotificationPreferenceRequest { ItemName = itemName, IsEnabled = false });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task SetAsync_EmptyUser_IsInvalidInput()
        {
            Result<bool> result = await PreferenceCommand().SetAsync(Guid.Empty,
                new SetNotificationPreferenceRequest { ItemName = "PushChannel" });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task SetAsync_NotFound_WhenNoRowStored()
        {
            Result<bool> result = await PreferenceCommand(saved: false).SetAsync(User,
                new SetNotificationPreferenceRequest { ItemName = "PushChannel" });

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        //.// 프로필 공개 항목 — 타인 프로필 변경 차단

        private static SoccerPlayerFieldVisibilityCommand VisibilityCommand(bool applied = true)
        {
            var repo = new Mock<IPlayerRepository>();
            repo.Setup(r => r.SetFieldVisibilityAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(applied));
            return new SoccerPlayerFieldVisibilityCommand(repo.Object, NullLogger<SoccerPlayerFieldVisibilityCommand>.Instance);
        }

        [Theory]
        [InlineData("Profile")]
        [InlineData("Height")]
        [InlineData("Weight")]
        [InlineData("PreferredFoot")]
        [InlineData("School")]
        [InlineData("GuardianPhone")]
        [InlineData("StrengthTags")]
        public async Task ExecuteAsync_SavesAllowedFields(string fieldName)
        {
            Result<bool> result = await VisibilityCommand().ExecuteAsync(User, fieldName, isPublic: false);

            result.IsSuccess.Should().BeTrue();
        }

        [Theory]
        [InlineData("0")]
        [InlineData("5")]
        [InlineData("Unknown")]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task ExecuteAsync_NumericAndUnknownFields_AreInvalidInput(string? fieldName)
        {
            Result<bool> result = await VisibilityCommand().ExecuteAsync(User, fieldName!, isPublic: true);

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task ExecuteAsync_EmptyUser_IsUnauthorized()
        {
            Result<bool> result = await VisibilityCommand().ExecuteAsync(Guid.Empty, "Height", isPublic: true);

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task ExecuteAsync_NotFound_WhenPlayerNotOwned()
        {
            // 타인 프로필 시도 — 존재 여부를 노출하지 않는다
            Result<bool> result = await VisibilityCommand(applied: false).ExecuteAsync(User, "Height", isPublic: true);

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        [Fact]
        public async Task ExecuteAsync_NormalizesFieldNameToEnumForm()
        {
            var repo = new Mock<IPlayerRepository>();
            string? captured = null;
            repo.Setup(r => r.SetFieldVisibilityAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, string, bool, Guid?, CancellationToken>((_, name, _, _, _) => captured = name)
                .ReturnsAsync(Result<bool>.Success(true));

            await new SoccerPlayerFieldVisibilityCommand(repo.Object, NullLogger<SoccerPlayerFieldVisibilityCommand>.Instance).ExecuteAsync(User, "Height", isPublic: true);

            captured.Should().Be("Height");
        }
    }
}
