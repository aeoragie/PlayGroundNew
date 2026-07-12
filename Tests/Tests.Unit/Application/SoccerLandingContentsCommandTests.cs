using Moq;
using PlayGround.Shared.Result;
using PlayGround.Contracts.Landing;
using PlayGround.Application.Interfaces;
using PlayGround.Application.Landing.Commands;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    public class SoccerLandingContentsCommandTests
    {
        [Fact]
        public async Task ExecuteAsync_ReturnsRepositoryResult()
        {
            var response = new LandingContentsResponse
            {
                Features = { new LandingItemDto { Title = "F1" } },
                Steps = { new LandingItemDto { Title = "S1" } }
            };
            var repo = new Mock<ILandingContentRepository>();
            repo.Setup(r => r.GetContentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<LandingContentsResponse>.Success(response));

            var query = new SoccerLandingContentsCommand(repo.Object);
            var result = await query.ExecuteAsync();

            Assert.True(result.IsSuccess);
            Assert.Single(result.Value!.Features);
            Assert.Single(result.Value!.Steps);
        }

        [Fact]
        public async Task ExecuteAsync_PropagatesFailure()
        {
            var repo = new Mock<ILandingContentRepository>();
            repo.Setup(r => r.GetContentsAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<LandingContentsResponse>.Error(ErrorCode.DatabaseError));

            var query = new SoccerLandingContentsCommand(repo.Object);
            var result = await query.ExecuteAsync();

            Assert.True(result.IsError);
            Assert.Equal(ErrorCode.DatabaseError, result.ResultData.DetailCode);
        }
    }
}
