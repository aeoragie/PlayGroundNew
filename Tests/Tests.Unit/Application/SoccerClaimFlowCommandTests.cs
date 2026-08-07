using FluentAssertions;
using Moq;
using Xunit;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Claim;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Claim.Commands;

namespace PlayGround.Tests.Unit.Application
{
    /// <summary>보호자 Claim 플로우 — 초대코드 경유와 공개 프로필 경유 두 갈래.
    /// **무효한 코드·연결 불가 선수는 사유를 구분하지 않는다**(코드 추측·존재 여부 탐지 대비).</summary>
    public class SoccerClaimFlowCommandTests
    {
        private static readonly Guid User = Guid.NewGuid();

        private sealed class Harness
        {
            public Mock<IClaimRepository> Repository { get; } = new();
            public SoccerClaimFlowCommand Command => new(Repository.Object);

            /// <summary>프로시저에 실제로 전달된 정규화 코드·관계·이름.</summary>
            public string? Code { get; private set; }
            public string? Relation { get; private set; }
            public string? Name { get; private set; }

            public Harness(ClaimInviteCardResponse? card = null, ClaimRequestSummaryResponse? created = null)
            {
                Repository.Setup(r => r.GetInviteCardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<string, CancellationToken>((code, _) => Code = code)
                    .ReturnsAsync(Result<ClaimInviteCardResponse?>.Success(card));
                Repository.Setup(r => r.CreateRequestAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<Guid, string, string, string, CancellationToken>((_, name, code, relation, _) =>
                    {
                        Name = name;
                        Code = code;
                        Relation = relation;
                    })
                    .ReturnsAsync(Result<ClaimRequestSummaryResponse?>.Success(created));
                Repository.Setup(r => r.CreateRequestByPlayerAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .Callback<Guid, string, Guid, string, CancellationToken>((_, name, _, relation, _) =>
                    {
                        Name = name;
                        Relation = relation;
                    })
                    .ReturnsAsync(Result<ClaimRequestSummaryResponse?>.Success(created));
                Repository.Setup(r => r.GetClaimCardBySlugAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<ClaimInviteCardResponse?>.Success(card));
            }
        }

        private static CreateClaimRequestRequest Request(string code = "ABCD12", string relation = "Mother") =>
            new() { Code = code, Relation = relation };

        //.// 코드 정규화

        [Theory]
        [InlineData("abcd12", "ABCD12")]      // 소문자 입력을 대문자로
        [InlineData("  abcd12  ", "ABCD12")]  // 앞뒤 공백 제거
        public async Task LookupAsync_NormalizesCodeBeforeLookup(string input, string expected)
        {
            var harness = new Harness(card: new ClaimInviteCardResponse());

            await harness.Command.LookupAsync(input);

            harness.Code.Should().Be(expected);
        }

        [Theory]
        [InlineData("abc")]                  // 4자 미만
        [InlineData("abcdefghijklm")]        // 12자 초과
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public async Task LookupAsync_SkipsLookup_ForOutOfRangeLength(string? code)
        {
            var harness = new Harness();

            Result<ClaimInviteCardResponse> result = await harness.Command.LookupAsync(code!);

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
            harness.Repository.Verify(r => r.GetInviteCardAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task LookupAsync_UnknownCode_IsNotFound_WithoutReason()
        {
            // 만료·사용됨·오타를 구분해 주면 코드 추측이 쉬워진다
            Result<ClaimInviteCardResponse> result = await new Harness().Command.LookupAsync("ABCD12");

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        //.// 요청 생성 — 관계 화이트리스트

        [Theory]
        [InlineData("Mother")]
        [InlineData("Father")]
        [InlineData("Guardian")]
        public async Task CreateAsync_AcceptsAllowedRelations(string relation)
        {
            var harness = new Harness(created: new ClaimRequestSummaryResponse());

            Result<ClaimRequestSummaryResponse> result =
                await harness.Command.CreateAsync(User, "김보호", Request(relation: relation));

            result.IsSuccess.Should().BeTrue();
            harness.Relation.Should().Be(relation);
        }

        [Theory]
        [InlineData("0")]        // 숫자 문자열이 enum으로 파싱되는 것을 막는다
        [InlineData("2")]
        [InlineData("Uncle")]
        [InlineData("")]
        [InlineData(null)]
        public async Task CreateAsync_UnknownRelation_IsInvalidInput(string? relation)
        {
            Result<ClaimRequestSummaryResponse> result =
                await new Harness().Command.CreateAsync(User, "김보호", Request(relation: relation!));

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task CreateAsync_EmptyUser_IsUnauthorized()
        {
            Result<ClaimRequestSummaryResponse> result =
                await new Harness().Command.CreateAsync(Guid.Empty, "김보호", Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Theory]
        [InlineData("  김보호  ", "김보호")]
        [InlineData("", "보호자")]        // 이름이 없으면 기본 호칭으로 스냅샷을 남긴다
        [InlineData("   ", "보호자")]
        public async Task CreateAsync_TrimsRequesterName_AndUsesDefaultWhenEmpty(string input, string expected)
        {
            var harness = new Harness(created: new ClaimRequestSummaryResponse());

            await harness.Command.CreateAsync(User, input, Request());

            harness.Name.Should().Be(expected);
        }

        [Fact]
        public async Task CreateAsync_NotFound_WhenCodeInvalid()
        {
            Result<ClaimRequestSummaryResponse> result =
                await new Harness().Command.CreateAsync(User, "김보호", Request());

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        //.// 공개 프로필 경유 (코드 없음)

        [Fact]
        public async Task LookupBySlugAsync_EmptySlug_IsInvalidInput()
        {
            Result<ClaimInviteCardResponse> result = await new Harness().Command.LookupBySlugAsync("   ");

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        [Fact]
        public async Task LookupBySlugAsync_NotFound_WhenAlreadyClaimedOrMissing()
        {
            // 이미 연결됨/없음을 구분하면 선수 존재 여부가 새어 나간다
            Result<ClaimInviteCardResponse> result = await new Harness().Command.LookupBySlugAsync("kim-yuhan-a1b2c3");

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        [Fact]
        public async Task CreateByPlayerAsync_EmptyPlayer_IsUnauthorized()
        {
            Result<ClaimRequestSummaryResponse> result =
                await new Harness().Command.CreateByPlayerAsync(User, "김보호", Guid.Empty, "Mother");

            result.ResultData.DetailCode.Should().Be(ErrorCode.Unauthorized);
        }

        [Fact]
        public async Task CreateByPlayerAsync_SharesRelationWhitelistWithCodePath()
        {
            Result<ClaimRequestSummaryResponse> result =
                await new Harness().Command.CreateByPlayerAsync(User, "김보호", Guid.NewGuid(), "0");

            result.ResultData.DetailCode.Should().Be(ErrorCode.InvalidInput);
        }

        //.// 취소·복원

        [Fact]
        public async Task CancelAsync_Forbidden_WhenRepositoryRejects()
        {
            // 남의 요청이거나 이미 처리된 요청
            var harness = new Harness();
            harness.Repository.Setup(r => r.CancelRequestAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            Result<bool> result = await harness.Command.CancelAsync(User, Guid.NewGuid());

            result.ResultData.DetailCode.Should().Be(ErrorCode.Forbidden);
        }

        [Fact]
        public async Task GetMineAsync_NotFound_WhenNoRequest()
        {
            // 클라이언트는 이걸 보고 스텝 ①부터 시작한다
            var harness = new Harness();
            harness.Repository.Setup(r => r.GetOwnRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ClaimRequestSummaryResponse?>.Success(null));

            Result<ClaimRequestSummaryResponse> result = await harness.Command.GetMineAsync(User);

            result.ResultData.DetailCode.Should().Be(ErrorCode.NotFound);
        }

        [Fact]
        public async Task GetMineAsync_ReturnsRequestAsIs()
        {
            var summary = new ClaimRequestSummaryResponse();
            var harness = new Harness();
            harness.Repository.Setup(r => r.GetOwnRequestAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<ClaimRequestSummaryResponse?>.Success(summary));

            Result<ClaimRequestSummaryResponse> result = await harness.Command.GetMineAsync(User);

            result.IsSuccess.Should().BeTrue();
            result.Value.Should().BeSameAs(summary);
        }
    }
}
