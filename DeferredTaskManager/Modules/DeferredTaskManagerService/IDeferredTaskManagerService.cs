using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <summary>
    /// Allows you to use multiple background tasks (or "runners") for deferred processing of consolidated data. 
    /// Runners are based on the PubSub template for asynchronous waiting for new tasks, 
    /// which makes this approach more reactive but less resource-intensive.
    /// </summary>
    /// <typeparamref name="T"></typeparamref>
    public interface IDeferredTaskManagerService<T> : IEventStorage<T>, IEventSenderInfo<T>
    {
        /// <summary>
        /// Sending available events to the delegate for on-demand processing
        /// </summary>
        void SendEvents();

        /// <summary>
        /// Launching Deferred Task Manager
        /// </summary>
        /// <paramref name="cancellationToken"/>
        Task StartAsync(Func<List<T>, CancellationToken, Task> eventConsumer,
            Func<List<T>, Exception, int, CancellationToken, Task>? eventConsumerRetryExhausted = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Launching Deferred Task Manager
        /// </summary>
        /// <paramref name="cancellationToken"/>
        Task StartAsync(Func<List<T>, CancellationToken, Task> eventConsumer,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Number of free runners in the pool
        /// </summary>
        int FreePoolCount { get; }

        /// <summary>
        /// Number of employed runners in the pool
        /// </summary>
        int EmployedPoolCount { get; }

        /// <summary>
        /// DTM instance creation time
        /// </summary>
        DateTimeOffset CreatedAt { get; }

        /// <summary>
        /// Check if there was activity in the instance within the specified time threshold
        /// </summary>
        /// <param name="inactivityThreshold">Inactivity threshold</param>
        /// <returns></returns>
        bool IsInactive(TimeSpan inactivityThreshold);
    }
}
