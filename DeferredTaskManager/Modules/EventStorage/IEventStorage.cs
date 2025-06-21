﻿using System;
using System.Collections.Generic;

namespace DTM
{
    /// <summary>
    /// Allows you to use multiple background tasks (or "runners") for deferred processing of consolidated data. 
    /// Runners are based on the PubSub template for asynchronous waiting for new tasks, 
    /// which makes this approach more reactive but less resource-intensive.
    /// </summary>
    /// <typeparamref name="T"></typeparamref>
    public interface IEventStorage<T>
    {
        /// <summary>
        /// Adding an event to be sent for processing
        /// </summary>
        /// <param name="event">Event for deferred processing</param>
        /// <param name="sendEvents">Sending events to the delegate for processing</param>
        void Add(T @event, bool sendEvents = true);

        /// <summary>
        /// Adding a collection of events to be sent for processing
        /// </summary>
        /// <param name="events">Events for deferred processing</param>
        /// <param name="sendEvents">Sending events to the delegate for processing</param>
        void Add(IEnumerable<T> events, bool sendEvents = true);

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
        /// Returns a value indicating whether the event storage is empty
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// Date and time of the last call to the method to add the value
        /// </summary>
        DateTimeOffset LastAddedAt { get; }
    }
}
