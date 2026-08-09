using Akka.Actor;
using NLog;

namespace PlayGround.Infrastructure.Actor
{
    public abstract class ReceiveActorBase : ReceiveActor
    {
        protected readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        protected readonly IServiceProvider ServiceProvider;

        protected ReceiveActorBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        protected void RegisterHandlerAsync<TMessage>(Func<TMessage, Task> handler)
        {
            ReceiveAsync(handler);
        }

        protected void RegisterHandler<TMessage>(Action<TMessage> handler)
        {
            Receive(handler);
        }
    }
}
