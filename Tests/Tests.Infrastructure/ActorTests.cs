using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlayGround.Infrastructure.Actor;
using Xunit;

namespace PlayGround.Tests.Infrastructure
{
    public class ActorTests : IAsyncLifetime
    {
        private ServiceProvider mServiceProvider = null!;
        private AkkaService mAkkaService = null!;

        public async ValueTask InitializeAsync()
        {
            mServiceProvider = new ServiceCollection().BuildServiceProvider();
            var configuration = new ConfigurationBuilder().Build();

            mAkkaService = new AkkaService(mServiceProvider, configuration, new FakeLifetime());
            await mAkkaService.StartAsync(CancellationToken.None);
        }

        public async ValueTask DisposeAsync()
        {
            await mAkkaService.StopAsync(CancellationToken.None);
            await mServiceProvider.DisposeAsync();
        }

        [Fact]
        public async Task SendAsync_EchoActor_ReturnsRequestData()
        {
            var actor = mAkkaService.CreateActor<EchoActor>("echo");
            Assert.NotNull(actor);

            var message = new ActorMessage<string, EchoPayload> { RequestData = "hello" };
            var result = await actor.SendAsync(message, TimeSpan.FromSeconds(5));

            Assert.True(result.IsSuccess);
            Assert.Equal("hello", result.ResultData?.Text);
        }

        [Fact]
        public async Task SendAsync_NoReply_ReturnsTimeoutCode()
        {
            var actor = mAkkaService.CreateActor<SilentActor>("silent");
            Assert.NotNull(actor);

            var result = await actor.SendAsync(new ActorMessage(), TimeSpan.FromMilliseconds(300));

            Assert.False(result.IsSuccess);
            Assert.Equal(ActorResultCode.Timeout, result.ResultCode);
        }

        [Fact]
        public void CreateActor_DuplicateName_ReturnsNullWithoutOrphan()
        {
            var first = mAkkaService.CreateActor<EchoActor>("dup");
            var second = mAkkaService.CreateActor<EchoActor>("dup");

            Assert.NotNull(first);
            Assert.Null(second);
            Assert.Same(first, mAkkaService.GetActor("dup"));
        }

        [Fact]
        public async Task CreateRouter_RoundRobin_ProcessesMessages()
        {
            var router = mAkkaService.CreateRouter<EchoActor>("pool", poolSize: 3);
            Assert.NotNull(router);

            var result = await router.SendAsync(
                new ActorMessage<string, EchoPayload> { RequestData = "routed" }, TimeSpan.FromSeconds(5));

            Assert.True(result.IsSuccess);
            Assert.Equal("routed", result.ResultData?.Text);
        }

        //.// Test Actors

        private class EchoPayload
        {
            public string Text { get; set; } = string.Empty;
        }

        private class EchoActor : ReceiveActorBase
        {
            public EchoActor(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                RegisterHandler<ActorMessage<string, EchoPayload>>(message =>
                {
                    message.ResultData = new EchoPayload { Text = message.RequestData ?? string.Empty };
                    Sender.Response(message);
                });
            }
        }

        private class SilentActor : ReceiveActorBase
        {
            public SilentActor(IServiceProvider serviceProvider) : base(serviceProvider)
            {
                RegisterHandler<ActorMessage>(_ => { });
            }
        }

        private class FakeLifetime : IHostApplicationLifetime
        {
            public CancellationToken ApplicationStarted => CancellationToken.None;
            public CancellationToken ApplicationStopping => CancellationToken.None;
            public CancellationToken ApplicationStopped => CancellationToken.None;

            public void StopApplication()
            {
            }
        }
    }
}
