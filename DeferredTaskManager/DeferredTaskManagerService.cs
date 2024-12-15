using DeferredTaskManager.CollectionStrategy;
using DeferredTaskManager.Enum;
using DeferredTaskManager.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DeferredTaskManager
{
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly ReaderWriterLockSlim _lockBag = new ReaderWriterLockSlim();
        private readonly object _locksIsStarted = new object();
        private readonly PubSub _pubSub = new PubSub();

        public int TaskCount => _collectionStrategy.Count;
        public int SubscribersCount => _pubSub.SubscribersCount;

        private ICollectionStrategy<T> _collectionStrategy = default!;
        private Func<List<T>, CancellationToken, Task> _taskFactory = default!;
        private int _retry;
        private int _millisecondsRetryDelay;
        private bool _isStarted = false;

        public DeferredTaskManagerService()
        {
        }

        public Task StartAsync(Func<List<T>, CancellationToken, Task> taskFactory,
            int taskPoolSize = 1000, CollectionType collectionType = CollectionType.Queue,
            int retry = 0, int millisecondsRetryDelay = 100,
            CancellationToken cancellationToken = default)
            => Execute(taskFactory, taskPoolSize, retry, millisecondsRetryDelay, collectionType, cancellationToken);

        public Task StartAsync(Func<List<T>, CancellationToken, Task> taskFactory,
            int taskPoolSize = 1000, int retry = 0, int millisecondsRetryDelay = 100, CancellationToken cancellationToken = default)
            => Execute(taskFactory, taskPoolSize, retry, millisecondsRetryDelay, CollectionType.Queue, cancellationToken);

        public void Add(T @event)
        {
            _lockBag.EnterReadLock();

            try
            {
                _collectionStrategy.Add(@event);
            }
            finally
            {
                _lockBag.ExitReadLock();
            }

            _pubSub.SendEvents();
        }

        public void Add(IEnumerable<T> events)
        {
            _lockBag.EnterReadLock();

            try
            {
                foreach (var ev in events)
                    _collectionStrategy.Add(ev);
            }
            finally
            {
                _lockBag.ExitReadLock();
            }

            _pubSub.SendEvents();
        }

        private List<T> GetAndClearBag()
        {
            List<T> result;

            _lockBag.EnterWriteLock();

            try
            {
                result = _collectionStrategy.GetItems().ToList();

                _collectionStrategy.Clear();
            }
            finally
            {
                _lockBag.ExitWriteLock();
            }

            return result;
        }

        private Task Execute(Func<List<T>, CancellationToken, Task> taskFactory, int taskPoolSize = 1000, int retry = 0, int millisecondsRetryDelay = 100, CollectionType collectionType = CollectionType.Queue, CancellationToken cancellationToken = default)
        {
            lock (_locksIsStarted)
            {
                if (_isStarted) return Task.CompletedTask;
                _isStarted = true;
            }

            _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));

            _collectionStrategy = collectionType switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };

            _retry = retry;

            _millisecondsRetryDelay = millisecondsRetryDelay;

            var taskPool = new List<Task>();

            for (int i = 0; i < taskPoolSize; i++)
            {
                taskPool.Add(SenderAsync(cancellationToken));
            }

            return Task.WhenAll(taskPool);
        }

        private async Task SenderAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                _pubSub.Subscribe(out Guid subscriberKey, out Task<bool> task);

                if (_collectionStrategy.IsEmpty)
                {
                    try
                    {
                        await task.WaitAsync(cancellationToken);
                    }
                    catch
                    {
                        _pubSub.Unsubscribe(subscriberKey);

                        continue;
                    }
                }
                else
                {
                    _pubSub.Unsubscribe(subscriberKey);
                }

                var events = GetAndClearBag();

                if (events.Count == 0) continue;

                await SendEventsAsync(events, _retry, cancellationToken);
            }
        }

        private async Task SendEventsAsync(List<T> events, int retry, CancellationToken cancellationToken)
        {
            try
            {
                await _taskFactory(events, cancellationToken);
            }
            catch
            {
                if (retry == 0) return;

                await Task.Delay(_millisecondsRetryDelay, cancellationToken);

                await SendEventsAsync(events, retry - 1, cancellationToken);
            }
        }
    }
}
