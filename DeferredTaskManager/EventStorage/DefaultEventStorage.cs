using DTM.CollectionStrategy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTM
{
    /// <inheritdoc/>
    public class DefaultEventStorage<T> : IEventStorage<T>
    {
        private readonly ReaderWriterLockSlim _collectionLock = new ReaderWriterLockSlim();
        private readonly Action _sendEventsSignal;

        private ICollectionStrategy<T> _collectionStrategy = default!;

        /// <inheritdoc/>
        public int Count => _collectionStrategy.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _collectionStrategy.IsEmpty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="sendEventsSignal"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public DefaultEventStorage(CollectionType type, Action sendEventsSignal)
        {
            _sendEventsSignal = sendEventsSignal ?? throw new ArgumentNullException(nameof(sendEventsSignal));
            InitializeCollectionStrategy(type);
        }

        /// <inheritdoc/>
        public virtual void Add(T @event, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                _collectionStrategy.Add(@event);
            });

            if (sendEvents)
                _sendEventsSignal();
        }

        /// <inheritdoc/>
        public virtual void Add(IEnumerable<T> events, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                foreach (var ev in events)
                    _collectionStrategy.Add(ev);
            });

            if (sendEvents)
                _sendEventsSignal();
        }

        /// <inheritdoc/>
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

        private void InitializeCollectionStrategy(CollectionType type)
        {
            _collectionStrategy = type switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };
        }
    }
}