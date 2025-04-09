using System;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Interface for interacting with the pool of available background runners
    /// </summary>
    public interface IPoolPubSub
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
        /// <param name="subscriberKey"></param>
        /// <param name="task"></param>
        void Subscribe(out Guid subscriberKey, out Task<bool> task);
        /// <summary>
        /// Deleting a runner
        /// </summary>
        /// <param name="subscriberKey"></param>
        void Unsubscribe(Guid subscriberKey);
    }
}
