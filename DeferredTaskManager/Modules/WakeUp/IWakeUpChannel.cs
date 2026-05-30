using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Interface for interacting with the pool of available background runners
    /// </summary>
    public interface IWakeUpChannel
    {
        /// <summary>
        /// Number of available runners in the pool
        /// </summary>
        int SubscribersCount { get; }
        /// <summary>
        /// Send a notification to any free runner from the pool that he needs to start execution
        /// </summary>
        void SendEvents();
        /// <summary>
        /// Adding a runner available for work
        /// </summary>
        /// <param name="cancellationToken">Cancellation Token</param>
        Task<bool> WaitForSignalAsync(CancellationToken cancellationToken);
    }
}
