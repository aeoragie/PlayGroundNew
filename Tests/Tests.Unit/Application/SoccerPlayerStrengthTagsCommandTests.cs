using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Commands;
using PlayGround.Contracts.Player;
using PlayGround.Shared.Result;
using System.Text.Json;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>강점 태그 저장 — 개수·길이·연락처 차단이 이 계층의 몫이다.
    /// 저장 화이트리스트라 클라이언트 인라인 검증을 우회해도 서버가 같은 기준으로 막아야 한다.</summary>
    public class SoccerPlayerStrengthTagsCommandTests
    {
        private static readonly Guid User = Guid.NewGuid();

        /// <summary>저장을 받아들이는 저장소 + 실제 전달된 JSON을 잡아 둔다.</summary>
        private static (SoccerPlayerStrengthTagsCommand Command, Func<string?> SavedJson) Create(bool applied = true)
        {
            string? captured = null;
            var repo = new Mock<IPlayerRepository>();
            repo.Setup(r => r.SaveStrengthTagsAsync(It.IsAny<Guid>(), It.IsAny<string?>(), It.IsAny<Guid?>(), It.IsAny<CancellationToken>()))
                .Callback<Guid, string?, Guid?, CancellationToken>((_, json, _, _) => captured = json)
                .ReturnsAsync(Result<bool>.Success(applied));
            return (new SoccerPlayerStrengthTagsCommand(repo.Object, NullLogger<SoccerPlayerStrengthTagsCommand>.Instance), () => captured);
        }

        private static List<string> Saved(Func<string?> json) =>
            JsonSerializer.Deserialize<List<string>>(json()!)!;

        //.// 인가

        [Fact]
        public async Task SaveAsync_EmptyUser_IsUnauthorized()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(Guid.Empty, new SaveStrengthTagsRequest());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task SaveAsync_Forbidden_WhenNotApplied()
        {
            // 소유 선수 없음 = 타인 프로필 시도. 존재 여부를 노출하지 않는다.
            (SoccerPlayerStrengthTagsCommand command, _) = Create(applied: false);

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["스피드"] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.Forbidden);
        }

        //.// 정규화

        [Fact]
        public async Task SaveAsync_TrimsWhitespaceAndLeadingHash()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["  #스피드 ", "# 왼발", "돌파"] });

            Saved(json).Should().Equal("스피드", "왼발", "돌파");
        }

        [Fact]
        public async Task SaveAsync_RemovesDuplicates_KeepingFirstOccurrence()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["스피드", "돌파", "스피드", "#돌파"] });

            Saved(json).Should().Equal("스피드", "돌파");
        }

        [Fact]
        public async Task SaveAsync_TreatsDifferentCaseAsDifferentTag()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["Speed", "speed"] });

            Saved(json).Should().Equal("Speed", "speed");
        }

        [Fact]
        public async Task SaveAsync_DropsEmptyAndHashOnlyValues()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["", "   ", "#", "# ", "스피드"] });

            Saved(json).Should().Equal("스피드");
        }

        [Fact]
        public async Task SaveAsync_PassesNull_WhenListEmptyMeansClear()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = [] });

            result.IsSuccess.Should().BeTrue();
            json().Should().BeNull();   // 프로시저가 NULL을 '없음'으로 처리한다
        }

        //.// 개수·길이

        [Fact]
        public async Task SaveAsync_AllowsUpToFiveTags()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = ["하나", "둘", "셋", "넷", "다섯"] });

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAsync_RejectsSixTags()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = ["하나", "둘", "셋", "넷", "다섯", "여섯"] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task SaveAsync_PassesWhenFiveRemainAfterDedup()
        {
            // 개수 판정은 정규화 뒤에 한다
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = ["하나", "둘", "셋", "넷", "다섯", "하나"] });

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAsync_OutOfRange_WhenLongerThanTwelveChars()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = [new string('가', 13)] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.OutOfRange);
        }

        [Fact]
        public async Task SaveAsync_AllowsExactlyTwelveChars()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = [new string('가', 12)] });

            result.IsSuccess.Should().BeTrue();
        }

        //.// 연락처·링크 차단

        [Theory]
        [InlineData("a@b.com")]              // 이메일
        [InlineData("연락 @김코치")]           // @ 포함
        [InlineData("http://me.kr")]         // 링크
        [InlineData("HTTPS로연락")]           // 대소문자 무시
        [InlineData("01012345678")]          // 8자리 이상 연속 숫자 = 전화·계좌
        public async Task SaveAsync_RejectsContactsAndLinks(string tag)
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = [tag] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData("2024우승")]      // 4자리 — 연도는 막지 않는다
        [InlineData("1234567")]      // 7자리 — 경계
        public async Task SaveAsync_DoesNotBlockShortNumbers(string tag)
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = [tag] });

            result.IsSuccess.Should().BeTrue();
        }
    }
}
