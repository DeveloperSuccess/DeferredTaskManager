using DTM.CollectionStrategy;
using DTM.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly ReaderWriterLockSlim _lockBag = new ReaderWriterLockSlim();
        private readonly object _locksIsStarted = new object();
        private readonly PubSub _pubSub = new PubSub();
        private readonly ICollectionStrategy<T> _collectionStrategy = default!;
        private readonly DeferredTaskManagerOptions<T> _dtmOptions;

        private bool _isStarted = false;

        public int TaskCount => _collectionStrategy.Count;
        public int SubscribersCount => _pubSub.SubscribersCount;

        public DeferredTaskManagerService(DeferredTaskManagerOptions<T> deferredTaskManagerOptions)
        {
            _dtmOptions = deferredTaskManagerOptions ?? throw new ArgumentNullException(nameof(deferredTaskManagerOptions));

            var context = new ValidationContext(deferredTaskManagerOptions, serviceProvider: null, items: null);

            Validator.ValidateObject(deferredTaskManagerOptions, context, true);

            _collectionStrategy = _dtmOptions.CollectionType switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };
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

        public async Task StartAsync(CancellationToken cancellationToken = default)
        {
            lock (_locksIsStarted)
            {
                if (_isStarted) return;
                _isStarted = true;
            }

            var taskPool = new List<Task>();

            for (int i = 0; i < _dtmOptions.TaskPoolSize; i++)
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

                await SendEventsAsync(events, _dtmOptions.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);
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
                await _dtmOptions.TaskFactory(events, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (retry == 0)
                {
                    if (_dtmOptions.RetryOptions.TaskFactoryRetryExhausted != null)
                    {
                        await _dtmOptions.RetryOptions.TaskFactoryRetryExhausted(events, cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                await Task.Delay(_dtmOptions.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);

                await SendEventsAsync(events, retry - 1, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}