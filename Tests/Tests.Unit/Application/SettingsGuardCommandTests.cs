using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Commands;
using PlayGround.Application.Settings.Commands;
using PlayGround.Domain.Account;
using PlayGround.Contracts.Settings;
using PlayGround.Domain.Soccer;
using PlayGround.Shared.Result;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>저장 화이트리스트 가드 2종 — 알림 설정 항목과 프로필 공개 항목.
    /// 어휘는 enum이 강제하고(승인형·미지 문자열은 컨버터가 Unknown으로 만든다 — LenientEnumJsonConverterTests),
    /// 여기서는 **Unknown이 저장까지 오면 서버가 거부**하는 것을 고정한다.</summary>
    public class SettingsGuardCommandTests
    {
        private static readonly Guid User = Guid.NewGuid();

        //.// 알림 설정 — 승인형은 enum에 없어 Unknown으로 떨어지고, 저장이 거부된다

        private static NotificationPreferenceCommand PreferenceCommand(bool saved = true)
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(r => r.SetNotificationPreferenceAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(saved));
            return new NotificationPreferenceCommand(repo.Object, NullLogger<NotificationPreferenceCommand>.Instance);
        }

        [Theory]
        [InlineData(NotificationPreferenceItem.PushChannel)]
        [InlineData(NotificationPreferenceItem.EmailChannel)]
        [InlineData(NotificationPreferenceItem.MatchResult)]
        [InlineData(NotificationPreferenceItem.Recruit)]
        [InlineData(NotificationPreferenceItem.Review)]
        [InlineData(NotificationPreferenceItem.VisitSummary)]
        public async Task SetAsync_SavesAllowedKeys(NotificationPreferenceItem itemName)
        {
            Result<bool> result = await PreferenceCommand().SetAsync(User,
                new SetNotificationPreferenceRequest { ItemName = itemName, IsEnabled = false });

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task SetAsync_UnknownItem_IsInvalidInput()
        {
            Result<bool> result = await PreferenceCommand().SetAsync(User,
                new SetNotificationPreferenceRequest { ItemName = NotificationPreferenceItem.Unknown, IsEnabled = false });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task SetAsync_EmptyUser_IsInvalidInput()
        {
            Result<bool> result = await PreferenceCommand().SetAsync(Guid.Empty,
                new SetNotificationPreferenceRequest { ItemName = NotificationPreferenceItem.PushChannel });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task SetAsync_NotFound_WhenNoRowStored()
        {
            Result<bool> result = await PreferenceCommand(saved: false).SetAsync(User,
                new SetNotificationPreferenceRequest { ItemName = NotificationPreferenceItem.PushChannel });

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
        [InlineData(SoccerPlayerProfileField.Profile)]
        [InlineData(SoccerPlayerProfileField.Height)]
        [InlineData(SoccerPlayerProfileField.Weight)]
        [InlineData(SoccerPlayerProfileField.PreferredFoot)]
        [InlineData(SoccerPlayerProfileField.School)]
        [InlineData(SoccerPlayerProfileField.GuardianPhone)]
        [InlineData(SoccerPlayerProfileField.StrengthTags)]
        public async Task ExecuteAsync_SavesAllowedFields(SoccerPlayerProfileField fieldName)
        {
            Result<bool> result = await VisibilityCommand().ExecuteAsync(User, fieldName, isPublic: false);

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_UnknownField_IsInvalidInput()
        {
            Result<bool> result = await VisibilityCommand().ExecuteAsync(User, SoccerPlayerProfileField.Unknown, isPublic: true);

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task ExecuteAsync_EmptyUser_IsUnauthorized()
        {
            Result<bool> result = await VisibilityCommand().ExecuteAsync(Guid.Empty, SoccerPlayerProfileField.Height, isPublic: true);

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task ExecuteAsync_NotFound_WhenPlayerNotOwned()
        {
            // 타인 프로필 시도 — 존재 여부를 노출하지 않는다
            Result<bool> result = await VisibilityCommand(applied: false).ExecuteAsync(User, SoccerPlayerProfileField.Height, isPublic: true);

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        [Fact]
        public async Task ExecuteAsync_StoresEnumMemberNameString()
        {
            var repo = new Mock<IPlayerRepository>();
            string? captured = null;
            repo.Setup(r => r.SetFieldVisibilityAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, string, bool, Guid?, CancellationToken>((_, name, _, _, _) => captured = name)
                .ReturnsAsync(Result<bool>.Success(true));

            await new SoccerPlayerFieldVisibilityCommand(repo.Object, NullLogger<SoccerPlayerFieldVisibilityCommand>.Instance)
                .ExecuteAsync(User, SoccerPlayerProfileField.Height, isPublic: true);

            captured.Should().Be("Height");
        }
    }
}
