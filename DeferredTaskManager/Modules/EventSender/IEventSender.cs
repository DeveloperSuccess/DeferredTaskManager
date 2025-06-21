using System;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Interface for interacting with the sender module
    /// </summary>
    /// <typeparamref name="T"/>
    public interface IEventSender<T> : IEventSenderInfo<T>
    {
        /// <summary>
        /// The task of running background tasks
        /// </summary>
        /// <param name="cancellationToken"></param>
        abstract Task StartBackgroundTasks(CancellationToken cancellationToken);
    }
}
