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
    public interface IDeferredTaskManagerService<T>
    {
        /// <summary>
        /// Adding an event to be sent for processing
        /// </summary>
        /// <param name="event">Event for deferred processing</param>
        void Add(T @event);

        /// <summary>
        /// Adding a collection of events to be sent for processing
        /// </summary>
        /// <param name="events">Events for deferred processing</param>
        void Add(IEnumerable<T> events);

        /// <summary>
        /// Adding an event without sending for processing
        /// </summary>
        /// <param name="event">Event for deferred processing</param>
        void AddWithoutSend(T @event);

        /// <summary>
        /// Adding a set of events without sending for processing
        /// </summary>
        /// <param name="events">Events for deferred processing</param>
        void AddWithoutSend(IEnumerable<T> events);

        /// <summary>
        /// Sending available events to the delegate for on-demand processing
        /// </summary>
        void SendEvents();

        /// <summary>
        /// Launching Deferred Task Manager
        /// </summary>
        /// <paramref name="deferredTaskManagerOptions"/>
        /// <paramref name="cancellationToken"/>
        Task StartAsync(DeferredTaskManagerOptions<T> deferredTaskManagerOptions, CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves collected events with further storage cleanup
        /// </summary>
        /// <returns>Collected events</returns>
        List<T> GetEventsAndClearStorage();

        /// <summary>
        /// Number of unprocessed events
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Number of free runners in the pool
        /// </summary>
        int FreePoolCount { get; }

        /// <summary>
        /// Number of employed runners in the pool
        /// </summary>
        int EmployedPoolCount { get; }
    }
}
