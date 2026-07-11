using Akka.Actor;
using NLog;

namespace PlayGround.Infrastructure.Actor
{
    /// <summary>
    /// 액터 베이스 클래스 (DI + 핸들러 등록)
    /// </summary>
    public abstract class ReceiveActorBase : ReceiveActor
    {
        protected readonly ILogger Logger = LogManager.GetCurrentClassLogger();
        protected readonly IServiceProvider ServiceProvider;

        protected ReceiveActorBase(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// 비동기 메시지 핸들러 등록
        /// </summary>
        protected void RegisterHandlerAsync<TMessage>(Func<TMessage, Task> handler)
        {
            ReceiveAsync(handler);
        }

        /// <summary>
        /// 동기 메시지 핸들러 등록
        /// </summary>
        protected void RegisterHandler<TMessage>(Action<TMessage> handler)
        {
            Receive(handler);
        }
    }
}
