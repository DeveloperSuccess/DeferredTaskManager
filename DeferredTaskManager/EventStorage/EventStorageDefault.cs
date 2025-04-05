using DTM.CollectionStrategy;
using DTM.EventStorage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTM
{
    /// <inheritdoc/>
    public class EventStorageDefault<T> : EventStorageAbstract<T>, IEventStorage<T>
    {
        private readonly ReaderWriterLockSlim _collectionLock = new ReaderWriterLockSlim();

        /// <inheritdoc/>
        public int Count => _collectionStrategy.Count;

        /// <inheritdoc/>
        public bool IsEmpty => _collectionStrategy.IsEmpty;

        /// <inheritdoc/>
        public virtual void Add(T @event, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                _collectionStrategy.Add(@event);
            });
        }

        /// <inheritdoc/>
        public virtual void Add(IEnumerable<T> events, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                foreach (var ev in events)
                    _collectionStrategy.Add(ev);
            });
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

        
    }
}