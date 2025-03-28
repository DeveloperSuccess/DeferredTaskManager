using BTM.CollectionStrategy;
using BTM.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BTM
{
    public class BackgroundTaskManagerService<T> : IBackgroundTaskManagerService<T>
    {
        private readonly ReaderWriterLockSlim _lockBag = new ReaderWriterLockSlim();
        private readonly object _locksIsStarted = new object();
        private readonly PubSub _pubSub = new PubSub();

        private ICollectionStrategy<T> _collectionStrategy = default!;
        private BackgroundTaskManagerOptions<T> _BTMOptions;
        private bool _isStarted = false;

        public int TaskCount => _collectionStrategy.Count;
        public int SubscribersCount => _pubSub.SubscribersCount;

        public BackgroundTaskManagerService()
        {

        }

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

        public async Task StartAsync(BackgroundTaskManagerOptions<T> BackgroundTaskManagerOptions, CancellationToken cancellationToken = default)
        {
            lock (_locksIsStarted)
            {
                if (_isStarted) return;
                _isStarted = true;
            }

            _BTMOptions = BackgroundTaskManagerOptions ?? throw new ArgumentNullException(nameof(BackgroundTaskManagerOptions));

            var context = new ValidationContext(BackgroundTaskManagerOptions, serviceProvider: null, items: null);

            Validator.ValidateObject(BackgroundTaskManagerOptions, context, true);

            _collectionStrategy = _BTMOptions.CollectionType switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };

            var taskPool = new List<Task>();

            for (int i = 0; i < _BTMOptions.TaskPoolSize; i++)
            {
                taskPool.Add(SenderAsync(cancellationToken));
            }

            await Task.WhenAll(taskPool).ConfigureAwait(false);
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
                        await task.WaitAsync(cancellationToken).ConfigureAwait(false);
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

                await SendEventsAsync(events, _BTMOptions.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);
            }
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

        private async Task SendEventsAsync(List<T> events, int retry, CancellationToken cancellationToken)
        {
            try
            {
                await _BTMOptions.TaskFactory(events, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (retry == 0)
                {
                    if (_BTMOptions.RetryOptions.TaskFactoryRetryExhausted != null)
                    {
                        await _BTMOptions.RetryOptions.TaskFactoryRetryExhausted(events, cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                await Task.Delay(_BTMOptions.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);

                await SendEventsAsync(events, retry - 1, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}