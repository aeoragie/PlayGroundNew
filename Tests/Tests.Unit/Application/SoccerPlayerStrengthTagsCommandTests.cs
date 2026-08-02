using System.Text.Json;
using FluentAssertions;
using Moq;
using Xunit;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Player;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Player.Commands;

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
            return (new SoccerPlayerStrengthTagsCommand(repo.Object), () => captured);
        }

        private static List<string> Saved(Func<string?> json) =>
            JsonSerializer.Deserialize<List<string>>(json()!)!;

        //.// 인가

        [Fact]
        public async Task SaveAsync_빈_사용자는_Unauthorized다()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(Guid.Empty, new SaveStrengthTagsRequest());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task SaveAsync_반영되지_않으면_Forbidden이다()
        {
            // 소유 선수 없음 = 타인 프로필 시도. 존재 여부를 노출하지 않는다.
            (SoccerPlayerStrengthTagsCommand command, _) = Create(applied: false);

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["스피드"] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.Forbidden);
        }

        //.// 정규화

        [Fact]
        public async Task SaveAsync_공백과_앞_해시를_정리한다()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["  #스피드 ", "# 왼발", "돌파"] });

            Saved(json).Should().Equal("스피드", "왼발", "돌파");
        }

        [Fact]
        public async Task SaveAsync_중복은_첫_등장_순서를_지키며_제거한다()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["스피드", "돌파", "스피드", "#돌파"] });

            Saved(json).Should().Equal("스피드", "돌파");
        }

        [Fact]
        public async Task SaveAsync_대소문자가_다르면_다른_태그다()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["Speed", "speed"] });

            Saved(json).Should().Equal("Speed", "speed");
        }

        [Fact]
        public async Task SaveAsync_빈_값과_해시만_있는_값은_버린다()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = ["", "   ", "#", "# ", "스피드"] });

            Saved(json).Should().Equal("스피드");
        }

        [Fact]
        public async Task SaveAsync_빈_목록은_태그를_비우는_뜻이라_null을_넘긴다()
        {
            (SoccerPlayerStrengthTagsCommand command, Func<string?> json) = Create();

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = [] });

            result.IsSuccess.Should().BeTrue();
            json().Should().BeNull();   // 프로시저가 NULL을 '없음'으로 처리한다
        }

        //.// 개수·길이

        [Fact]
        public async Task SaveAsync_다섯_개까지_허용한다()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = ["하나", "둘", "셋", "넷", "다섯"] });

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAsync_여섯_개는_거부한다()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = ["하나", "둘", "셋", "넷", "다섯", "여섯"] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task SaveAsync_중복_제거_후_다섯_개면_통과한다()
        {
            // 개수 판정은 정규화 뒤에 한다
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = ["하나", "둘", "셋", "넷", "다섯", "하나"] });

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task SaveAsync_열두_자를_넘으면_OutOfRange다()
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User,
                new SaveStrengthTagsRequest { Tags = [new string('가', 13)] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.OutOfRange);
        }

        [Fact]
        public async Task SaveAsync_열두_자는_통과한다()
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
        public async Task SaveAsync_연락처와_링크는_거부한다(string tag)
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = [tag] });

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData("2024우승")]      // 4자리 — 연도는 막지 않는다
        [InlineData("1234567")]      // 7자리 — 경계
        public async Task SaveAsync_짧은_숫자는_막지_않는다(string tag)
        {
            (SoccerPlayerStrengthTagsCommand command, _) = Create();

            Result<bool> result = await command.SaveAsync(User, new SaveStrengthTagsRequest { Tags = [tag] });

            result.IsSuccess.Should().BeTrue();
        }
    }
}
