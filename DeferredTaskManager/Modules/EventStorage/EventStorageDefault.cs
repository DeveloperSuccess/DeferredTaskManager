using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTM
{

    public class EventStorageDefault<T> : IEventStorage<T>
    {
        private readonly ReaderWriterLockSlim _collectionLock = new ReaderWriterLockSlim();
        private readonly DeferredTaskManagerOptions<T> _options;
        private readonly IStorageStrategy<T> _collectionStrategy;

        public DateTimeOffset LastAddedAt { get; private set; } = DateTimeOffset.MinValue;


        public EventStorageDefault(IOptions<DeferredTaskManagerOptions<T>> options, IStorageStrategy<T> collectionStrategy)
        {
            _options = options.Value;
            _collectionStrategy = collectionStrategy;
        }

        public int Count => _collectionStrategy.Count;

        public bool IsEmpty => _collectionStrategy.IsEmpty;

        public virtual void Add(T @event, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                _collectionStrategy.Add(@event);
            });
        }

        public virtual void Add(IEnumerable<T> events, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                foreach (var ev in events)
                    _collectionStrategy.Add(ev);
            });
        }

        public virtual List<T> GetEventsAndClearStorage()
        {
            List<T> items;

            _collectionLock.EnterWriteLock();

            try
            {
                items = _collectionStrategy.GetItems().ToList();

                _collectionStrategy.Clear();
            }
            finally
            {
                _collectionLock.ExitWriteLock();
            }

            return items;
        }

        private void ExecuteWithReadLock(Action action)
        {
            LastAddedAt = DateTimeOffset.UtcNow;

            _collectionLock.EnterReadLock();

            try
            {
                action();
            }
            finally
            {
                _collectionLock.ExitReadLock();
            }
        }
    }
}