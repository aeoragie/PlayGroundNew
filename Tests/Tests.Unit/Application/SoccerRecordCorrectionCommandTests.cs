using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Team.Commands;
using PlayGround.Contracts.Team;
using PlayGround.Shared.Result;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>공식 기록 수정 신청 — **생성·조회·취소만** 한다(설계 결정 6·7).
    /// 저장소가 null을 주는 경우(남의 경기·친선·중복)는 어느 쪽인지 노출하지 않고 일괄 Forbidden이어야 한다.</summary>
    public class SoccerRecordCorrectionCommandTests
    {
        private static readonly Guid Manager = Guid.NewGuid();
        private static readonly Guid Match = Guid.NewGuid();

        private static CreateRecordCorrectionRequest Request(
            string field = "Score", string requested = "2 : 1", string? current = " 3 : 1 ", string? description = null) =>
            new()
            {
                MatchId = Match,
                FieldType = field,
                RequestedValue = requested,
                CurrentValue = current,
                Description = description,
            };

        private static Mock<ISoccerTeamRepository> RepoCreating(Guid? created)
        {
            var repo = new Mock<ISoccerTeamRepository>();
            repo.Setup(r => r.CreateRecordCorrectionAsync(It.IsAny<Guid>(), It.IsAny<CreateRecordCorrectionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid?>.Success(created));
            return repo;
        }

        //.// 생성 — 인가·입력 가드

        [Fact]
        public async Task ExecuteAsync_EmptyUser_IsUnauthorized()
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Guid.Empty, Request());

            result.IsError.Should().BeTrue();
            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task ExecuteAsync_EmptyMatch_IsInvalidInput()
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);
            CreateRecordCorrectionRequest request = Request();
            request.MatchId = Guid.Empty;

            Result<Guid> result = await command.ExecuteAsync(Manager, request);

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData("0")]        // 숫자 문자열이 enum으로 파싱되는 것을 막는다
        [InlineData("Unknown")]
        [InlineData("")]
        public async Task ExecuteAsync_UnknownField_IsInvalidInput(string fieldType)
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Manager, Request(field: fieldType));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public async Task ExecuteAsync_EmptyRequestedValue_IsInvalidInput(string requested)
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Manager, Request(requested: requested));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task ExecuteAsync_RequestedValueOverLimit_IsInvalidInput()
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Manager, Request(requested: new string('가', 101)));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        //.// 생성 — 정규화

        [Fact]
        public async Task ExecuteAsync_NormalizesValuesBeforeSaving()
        {
            Mock<ISoccerTeamRepository> repo = RepoCreating(Guid.NewGuid());
            var command = new SoccerRecordCorrectionCommand(repo.Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);
            CreateRecordCorrectionRequest request = Request(
                field: "Score", requested: "  2 : 1  ", current: "  3 : 1  ", description: "   ");

            await command.ExecuteAsync(Manager, request);

            request.RequestedValue.Should().Be("2 : 1");
            request.CurrentValue.Should().Be("3 : 1");
            request.Description.Should().BeNull();      // 공백만 남는 설명은 없는 것으로 저장한다
            request.FieldType.Should().Be("Score");     // enum 이름으로 정규화
        }

        [Fact]
        public async Task ExecuteAsync_TruncatesDescriptionToLimit()
        {
            Mock<ISoccerTeamRepository> repo = RepoCreating(Guid.NewGuid());
            var command = new SoccerRecordCorrectionCommand(repo.Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);
            CreateRecordCorrectionRequest request = Request(description: new string('나', 600));

            await command.ExecuteAsync(Manager, request);

            request.Description.Should().HaveLength(500);
        }

        //.// 생성 — 저장소 결과 해석

        [Fact]
        public async Task ExecuteAsync_ReturnsRequestId_OnSuccess()
        {
            var created = Guid.NewGuid();
            var command = new SoccerRecordCorrectionCommand(RepoCreating(created).Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Manager, Request());

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().Be(created);
        }

        [Fact]
        public async Task ExecuteAsync_ForeignFriendlyDuplicate_AreForbidden_WithoutReason()
        {
            // 어느 쪽인지 알려주면 남의 경기 존재 여부가 새어 나간다
            var command = new SoccerRecordCorrectionCommand(RepoCreating(null).Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Manager, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task ExecuteAsync_MapsRepositoryFailureToDatabaseError()
        {
            var repo = new Mock<ISoccerTeamRepository>();
            repo.Setup(r => r.CreateRecordCorrectionAsync(It.IsAny<Guid>(), It.IsAny<CreateRecordCorrectionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid?>.Error(ErrorCode.DatabaseTimeout));
            var command = new SoccerRecordCorrectionCommand(repo.Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteAsync(Manager, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.DatabaseError);
        }

        //.// 보호자 경로 — 팀 경로와 같은 규칙 + 자녀 지정 필수

        [Fact]
        public async Task ExecuteByGuardianAsync_InvalidInput_WhenChildMissing()
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteByGuardianAsync(Manager, Guid.Empty, Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task ExecuteByGuardianAsync_Forbidden_WhenNotOwnChild()
        {
            var repo = new Mock<ISoccerTeamRepository>();
            repo.Setup(r => r.CreateGuardianCorrectionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CreateRecordCorrectionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Guid?>.Success(null));
            var command = new SoccerRecordCorrectionCommand(repo.Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<Guid> result = await command.ExecuteByGuardianAsync(Manager, Guid.NewGuid(), Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Forbidden);
        }

        //.// 취소 — 접수 상태의 내 신청만

        [Fact]
        public async Task CancelAsync_Forbidden_WhenRepositoryRejects()
        {
            // 이미 심사가 시작된 건·남의 신청 — 프로시저가 false를 준다
            var repo = new Mock<ISoccerTeamRepository>();
            repo.Setup(r => r.CancelRecordCorrectionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));
            var command = new SoccerRecordCorrectionCommand(repo.Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<bool> result = await command.CancelAsync(Manager, Guid.NewGuid());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task CancelAsync_CancelsWhilePending()
        {
            var repo = new Mock<ISoccerTeamRepository>();
            repo.Setup(r => r.CancelRecordCorrectionAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));
            var command = new SoccerRecordCorrectionCommand(repo.Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<bool> result = await command.CancelAsync(Manager, Guid.NewGuid());

            result.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public async Task CancelAsync_EmptyId_IsInvalidInput()
        {
            var command = new SoccerRecordCorrectionCommand(new Mock<ISoccerTeamRepository>().Object, NullLogger<SoccerRecordCorrectionCommand>.Instance);

            Result<bool> result = await command.CancelAsync(Manager, Guid.Empty);

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        //.// 설계 결정 보호

        [Fact]
        public void ApproveAndRejectMethods_DoNotExistInThisUseCase()
        {
            // 심사는 주최측(대회 운영 서비스)의 몫 — 여기에 생기면 설계 결정 7이 무너진다
            string[] methods = typeof(SoccerRecordCorrectionCommand)
                .GetMethods(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Select(m => m.Name)
                .ToArray();

            methods.Should().NotContain(n => n.Contains("Approve", StringComparison.OrdinalIgnoreCase));
            methods.Should().NotContain(n => n.Contains("Reject", StringComparison.OrdinalIgnoreCase));
            methods.Should().NotContain(n => n.Contains("Review", StringComparison.OrdinalIgnoreCase));
        }
    }
}
