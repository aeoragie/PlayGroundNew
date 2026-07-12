using Moq;
using PlayGround.Shared.Result;
using PlayGround.Application.Auth.Commands;
using PlayGround.Application.Auth.Models;
using PlayGround.Application.Interfaces;
using Xunit;

namespace PlayGround.Tests.Unit.Application
{
    public class LoginBySocialCommandTests
    {
        private static AccountUser SampleUser(Guid id) => new()
        {
            UserId = id,
            Email = "user@google.social",
            DisplayName = "구글유저",
            UserRole = "General",
            AuthProvider = "Google"
        };

        private static Mock<IJwtTokenService> TokenMock()
        {
            var token = new Mock<IJwtTokenService>();
            token.Setup(t => t.GenerateAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>()))
                .Returns("jwt-token");
            return token;
        }

        [Fact]
        public async Task ExistingUser_ReturnsToken_WithoutCreating()
        {
            var userId = Guid.NewGuid();
            var repo = new Mock<IAccountRepository>();
            repo.Setup(r => r.GetBySocialAsync("Google", "g-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AccountUser?>.Success(SampleUser(userId)));

            var command = new LoginBySocialCommand(repo.Object, TokenMock().Object);
            var result = await command.ExecuteAsync("Google", "g-1", "user@google.social", "구글유저", null);

            Assert.True(result.IsSuccess);
            Assert.Equal("jwt-token", result.Value!.AccessToken);
            Assert.Equal(userId, result.Value.User.UserId);
            repo.Verify(r => r.CreateWithSocialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task NewUser_CreatesThenReturnsToken()
        {
            var newId = Guid.NewGuid();
            var repo = new Mock<IAccountRepository>();
            repo.Setup(r => r.GetBySocialAsync("Google", "g-2", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AccountUser?>.Success(null));   // 미존재
            repo.Setup(r => r.CreateWithSocialAsync(It.IsAny<string>(), It.IsAny<string>(), "Google", "g-2", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AccountUser>.Success(SampleUser(newId)));

            var command = new LoginBySocialCommand(repo.Object, TokenMock().Object);
            var result = await command.ExecuteAsync("Google", "g-2", "user@google.social", "구글유저", null);

            Assert.True(result.IsSuccess);
            Assert.Equal(newId, result.Value!.User.UserId);
            repo.Verify(r => r.CreateWithSocialAsync(It.IsAny<string>(), It.IsAny<string>(), "Google", "g-2", It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task MissingProviderId_ReturnsError()
        {
            var command = new LoginBySocialCommand(new Mock<IAccountRepository>().Object, TokenMock().Object);

            var result = await command.ExecuteAsync("Google", "", null, null, null);

            Assert.True(result.IsError);
            Assert.Equal(ErrorCode.MissingRequired, result.ResultData.DetailCode);
        }

        [Fact]
        public async Task LookupError_Propagates_NoCreate()
        {
            var repo = new Mock<IAccountRepository>();
            repo.Setup(r => r.GetBySocialAsync("Kakao", "k-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<AccountUser?>.Error(ErrorCode.DatabaseError));

            var command = new LoginBySocialCommand(repo.Object, TokenMock().Object);
            var result = await command.ExecuteAsync("Kakao", "k-1", null, null, null);

            Assert.True(result.IsError);
            repo.Verify(r => r.CreateWithSocialAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
