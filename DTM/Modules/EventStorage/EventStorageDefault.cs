using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTM
{
    /// <inheritdoc/>
    public class EventStorageDefault<T> : IEventStorage<T>
    {
        private readonly DeferredTaskManagerOptions<T> _options;
        private readonly IQueueStorage<T> _collectionStrategy;

        /// <inheritdoc/>
        public DateTimeOffset LastAddedAt { get; private set; } = DateTimeOffset.MinValue;

        /// <inheritdoc/>
        public EventStorageDefault(IOptions<DeferredTaskManagerOptions<T>> options, IQueueStorage<T> collectionStrategy)
        {
            _options = options.Value;
            _collectionStrategy = collectionStrategy;
        }

        /// <inheritdoc/>
        public int Count => _collectionStrategy.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _collectionStrategy.IsEmpty;

        /// <inheritdoc/>
        public virtual void Add(T @event, bool sendEvents = true)
        {
            LastAddedAt = DateTimeOffset.UtcNow;
            _collectionStrategy.Add(@event);
        }

        /// <inheritdoc/>
        public virtual void Add(IEnumerable<T> events, bool sendEvents = true)
        {
            LastAddedAt = DateTimeOffset.UtcNow;
            foreach (var ev in events)
            {
                _collectionStrategy.Add(ev);
            }
        }

        /// <inheritdoc/>
        public virtual List<T> GetEventsAndClearStorage()
        {
            return _collectionStrategy.ExtractAll();
        }
    }
}