using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Interface for interacting with the sender module
    /// </summary>
    /// <typeparamref name="T"/>
    public interface IEventSender<T>
    {
        /// <summary>
        /// The task of running background tasks
        /// </summary>
        /// <param name="cancellationToken"></param>
        Task StartBackgroundTasks(CancellationToken cancellationToken);
    }
}
