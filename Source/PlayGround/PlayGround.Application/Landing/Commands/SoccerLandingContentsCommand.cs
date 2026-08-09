using Microsoft.Extensions.Logging;
using PlayGround.Application.Interfaces;
using PlayGround.Contracts.Landing;
using PlayGround.Shared.Logging;
using PlayGround.Shared.Result;
using System.Diagnostics;

namespace PlayGround.Application.Landing.Commands
{
    public class SoccerLandingContentsCommand
    {
        private readonly ILandingContentRepository mRepository;
        private readonly ILogger<SoccerLandingContentsCommand> mLogger;

        public SoccerLandingContentsCommand(ILandingContentRepository repository, ILogger<SoccerLandingContentsCommand> logger)
        {
            Debug.Assert(repository != null, "repository is required");
            mRepository = repository ?? throw new ArgumentNullException(nameof(repository));
            mLogger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Result<LandingContentsResponse>> ExecuteAsync(CancellationToken cancellation = default) =>
            (await ExecuteCoreAsync(cancellation)).LogWith(mLogger, "Execute");

        private async Task<Result<LandingContentsResponse>> ExecuteCoreAsync(CancellationToken cancellation = default)
        {
            return await mRepository.GetContentsAsync(cancellation);
        }
    }
}
