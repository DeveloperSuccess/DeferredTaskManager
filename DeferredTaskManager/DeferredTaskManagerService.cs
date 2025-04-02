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
    /// <inheritdoc/>
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly ReaderWriterLockSlim _collectionLock = new ReaderWriterLockSlim();
        private readonly object _startLock = new object();
        private readonly PubSub _pubSub = new PubSub();

        private ICollectionStrategy<T> _collectionStrategy = default!;
        private DeferredTaskManagerOptions<T> _options = default!;
        private bool _isStarted = false;

        /// <inheritdoc/>
        public int Count => _collectionStrategy.Count;

        /// <inheritdoc/>
        public int FreePoolCount => _pubSub.SubscribersCount;

        /// <inheritdoc/>
        public int EmployedPoolCount => _options.PoolSize - _pubSub.SubscribersCount;

        /// <inheritdoc/>
        public virtual void Add(T @event, bool sendEvents = true)
        {
            ExecuteWithReadLock(() =>
            {
                _collectionStrategy.Add(@event);
            });

            if (sendEvents)
                _pubSub.SendEvents();
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
                _pubSub.SendEvents();
        }

        /// <inheritdoc/>
        public virtual Task StartAsync(DeferredTaskManagerOptions<T> options, CancellationToken cancellationToken = default)
        {
            EnsureNotStarted();
            ValidateOptions(options);

            _options = options;
            InitializeCollectionStrategy();

            var tasks = CreateBackgroundTasks(cancellationToken);

            return Task.WhenAll(tasks);
        }

        /// <inheritdoc/>
        public virtual void SendEvents()
        {
            _pubSub.SendEvents();
        }

        /// <inheritdoc/>
        public virtual List<T> GetEventsAndClearStorage()
        {
            List<T> result;

            _collectionLock.EnterWriteLock();

            try
            {
                result = _collectionStrategy.GetItems().ToList();

                _collectionStrategy.Clear();
            }
            finally
            {
                _collectionLock.ExitWriteLock();
            }

            return result;
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

        private void EnsureNotStarted()
        {
            lock (_startLock)
            {
                if (_isStarted) throw new Exception($"{nameof(DeferredTaskManagerService<T>)} has already been launched.");
                _isStarted = true;
            }
        }

        /// <inheritdoc/>
        private async Task StartSendDelay(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!_options.SendDelayOptions.ConsiderDifference)
                {
                    _pubSub.SendEvents();

                    await Task.Delay((_options.SendDelayOptions.MillisecondsSendDelay), cancellationToken).ConfigureAwait(false);

                    continue;
                }

                Stopwatch stopWatch = new Stopwatch();

                stopWatch.Start();

                _pubSub.SendEvents();

                stopWatch.Stop();

                var delay = _options.SendDelayOptions.MillisecondsSendDelay - (int)stopWatch.ElapsedMilliseconds;

                await Task.Delay(delay > 0 ? delay : 0, cancellationToken).ConfigureAwait(false);
            }
        }

        private void ValidateOptions(DeferredTaskManagerOptions<T> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var context = new ValidationContext(options, serviceProvider: null, items: null);
            Validator.ValidateObject(options, context, true);
        }

        private void InitializeCollectionStrategy()
        {
            _collectionStrategy = _options.CollectionType switch
            {
                CollectionType.Bag => new BagStrategy<T>(),
                CollectionType.Queue => new QueueStrategy<T>(),
                _ => throw new ArgumentException("Unacceptable collection type"),
            };
        }

        private IEnumerable<Task> CreateBackgroundTasks(CancellationToken cancellationToken)
        {
            var tasks = new List<Task>();

            for (int i = 0; i < _options.PoolSize; i++)
            {
                tasks.Add(SenderAsync(cancellationToken));
            }

            if (_options.SendDelayOptions != null)
            {
                tasks.Add(StartSendDelay(cancellationToken));
            }

            return tasks;
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

                var events = GetEventsAndClearStorage();

                if (events.Count == 0) continue;

                await SendEventsAsync(events, _options.RetryOptions.RetryCount, cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task SendEventsAsync(List<T> events, int retryCount, CancellationToken cancellationToken)
        {
            try
            {
                await _options.TaskFactory(events, cancellationToken).ConfigureAwait(false);
            }
            catch
            {
                if (retryCount == 0)
                {
                    if (_options.RetryOptions.TaskFactoryRetryExhausted != null)
                    {
                        await _options.RetryOptions.TaskFactoryRetryExhausted(events, cancellationToken).ConfigureAwait(false);
                    }

                    return;
                }

                await Task.Delay(_options.RetryOptions.MillisecondsRetryDelay, cancellationToken).ConfigureAwait(false);

                await SendEventsAsync(events, retryCount - 1, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}