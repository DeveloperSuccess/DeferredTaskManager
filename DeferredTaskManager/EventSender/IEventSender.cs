using System.Collections.Generic;
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
        /// Creating background runners
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>Enumerator of background runners</returns>
        IEnumerable<Task> CreateBackgroundTasks(CancellationToken cancellationToken);
    }
}
