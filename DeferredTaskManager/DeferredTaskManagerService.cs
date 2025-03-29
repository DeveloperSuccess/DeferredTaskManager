using DTM.CollectionStrategy;
using DTM.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
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
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly ReaderWriterLockSlim _lockBag = new ReaderWriterLockSlim();
        private readonly object _locksIsStarted = new object();
        private readonly PubSub _pubSub = new PubSub();

        private ICollectionStrategy<T> _collectionStrategy = default!;
        private DeferredTaskManagerOptions<T> _dtmOptions = default!;
        private bool _isStarted = false;

        /// <summary>
        /// Number of unprocessed events
        /// </summary>
        public int Count => _collectionStrategy.Count;

        /// <summary>
        /// Number of free runners in the pool
        /// </summary>
        public int FreePoolCount => _pubSub.SubscribersCount;

        /// <summary>
        /// Number of employed runners in the pool
        /// </summary>
        public int EmployedPoolCount => _dtmOptions.PoolSize - _pubSub.SubscribersCount;

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

        public void AddWithoutSend(T @event)
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
        }

        public void AddWithoutSend(IEnumerable<T> events)
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
        }

        public async Task StartAsync(DeferredTaskManagerOptions<T> deferredTaskManagerOptions, CancellationToken cancellationToken = default)
        {
            lock (_locksIsStarted)
            {
                if (_isStarted) return;
                _isStarted = true;
            }

            _dtmOptions = deferredTaskManagerOptions ?? throw new ArgumentNullException(nameof(deferredTaskManagerOptions));

            var context = new ValidationContext(deferredTaskManagerOptions, serviceProvider: null, items: null);

            Validator.ValidateObject(deferredTaskManagerOptions, context, true);

            _collectionStrategy = _dtmOptions.CollectionType switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };

            var taskPool = new List<Task>();

            for (int i = 0; i < _dtmOptions.PoolSize; i++)
            {
                taskPool.Add(SenderAsync(cancellationToken));
            }

            if (deferredTaskManagerOptions.SendDelayOptions != null)
                taskPool.Add(StartSendDelay(cancellationToken));

            await Task.WhenAll(taskPool).ConfigureAwait(false);
        }

        public void SendEvents()
        {
            _pubSub.SendEvents();
        }

        public async Task StartSendDelay(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_dtmOptions.SendDelayOptions.ConsiderDifference)
                {
                    _pubSub.SendEvents();

                    await Task.Delay((_dtmOptions.SendDelayOptions.MillisecondsSendDelay), cancellationToken).ConfigureAwait(false);

                    continue;
                }

                Stopwatch stopWatch = new Stopwatch();

                stopWatch.Start();

                _pubSub.SendEvents();

                stopWatch.Stop();

                var delay = _dtmOptions.SendDelayOptions.MillisecondsSendDelay - (int)stopWatch.ElapsedMilliseconds;

                await Task.Delay(delay > 0 ? delay : 0, cancellationToken).ConfigureAwait(false);
            }
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

                await SendEventsAsync(events, _dtmOptions.RetryOptions.RetryCount, cancellationToken).ConfigureAwait(false);
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

        private async Task SendEventsAsync(List<T> events, int retryCount, CancellationToken cancellationToken)
        {
            try
            {
                await _dtmOptions.TaskFactory(events, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (retryCount == 0)
                {
                    if (_dtmOptions.RetryOptions.TaskFactoryRetryExhausted != null)
                    {
                        await _dtmOptions.RetryOptions.TaskFactoryRetryExhausted(events, cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                await Task.Delay(_dtmOptions.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);

                await SendEventsAsync(events, retryCount - 1, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}