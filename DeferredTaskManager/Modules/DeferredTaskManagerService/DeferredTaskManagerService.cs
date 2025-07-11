using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{
    /// <inheritdoc/>
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly object _startLock = new object();
        private readonly IPoolPubSub _pubSub;
        private readonly IEventStorage<T> _eventStorage;
        private readonly IEventSender<T> _eventSender;

        private readonly DeferredTaskManagerOptions<T> _options;
        
        /// <inheritdoc/>
        public DeferredTaskManagerService(IOptions<DeferredTaskManagerOptions<T>> options, IEventStorage<T> eventStorage, IEventSender<T> eventSender, IPoolPubSub pubSub)
        {
            _options = options.Value;
            _eventStorage = eventStorage;
            _eventSender = eventSender;
            _pubSub = pubSub;
        }

        /// <inheritdoc/>
        public bool IsStarted { get; private set; } = false;
        /// <inheritdoc/>
        public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

        #region Implemented methods IEventStorage
        /// <inheritdoc/>
        public int Count => _eventStorage.Count;
        /// <inheritdoc/>
        public bool IsEmpty => _eventStorage.IsEmpty;
        /// <inheritdoc/>
        public virtual void Add(T @event, bool sendEvents = true) => Add(() => _eventStorage.Add(@event), sendEvents);
        /// <inheritdoc/>
        public virtual void Add(IEnumerable<T> events, bool sendEvents = true) => Add(() => _eventStorage.Add(events), sendEvents);
        /// <inheritdoc/>
        public virtual List<T> GetEventsAndClearStorage() => _eventStorage.GetEventsAndClearStorage();
        /// <inheritdoc/>
        public DateTimeOffset LastAddedAt => _eventStorage.LastAddedAt;

        #endregion

        #region Implemented methods IPubSub
        /// <inheritdoc/>
        public int FreePoolCount => _pubSub.SubscribersCount;
        /// <inheritdoc/>
        public virtual void SendEvents() => _pubSub.SendEvents();
        /// <inheritdoc/>
        public int EmployedPoolCount => _options.PoolSize - _pubSub.SubscribersCount;

        #endregion

        #region Implemented methods IEventSender
        /// <inheritdoc/>
        public DateTimeOffset StartAt => _eventSender.StartAt;
        /// <inheritdoc/>
        public DateTimeOffset LastSendAt => _eventSender.LastSendAt;

        #endregion

        /// <inheritdoc/>
        public virtual Task StartAsync(Func<List<T>, CancellationToken, Task> eventConsumer,
            Func<List<T>, Exception, int, CancellationToken, Task>? eventConsumerRetryExhausted = null,
            CancellationToken cancellationToken = default) =>
            Initializing(eventConsumer, eventConsumerRetryExhausted, cancellationToken);

        /// <inheritdoc/>
        public virtual Task StartAsync(Func<List<T>, CancellationToken, Task> eventConsumer,
            CancellationToken cancellationToken = default) =>
            Initializing(eventConsumer, cancellationToken: cancellationToken);

        /// <inheritdoc/>
        public bool IsInactive(TimeSpan inactivityThreshold)
        {
            var now = DateTimeOffset.UtcNow;

            var recentActivity =
                now - LastSendAt <= inactivityThreshold ||
                now - LastAddedAt <= inactivityThreshold ||
                now - CreatedAt <= inactivityThreshold ||
                now - StartAt <= inactivityThreshold;

            return !recentActivity;
        }

        private Task Initializing(Func<List<T>, CancellationToken, Task> eventConsumer,
            Func<List<T>, Exception, int, CancellationToken, Task>? eventConsumerRetryExhausted = null,
            CancellationToken cancellationToken = default)
        {
            EnsureNotStarted();

            ValidateOptions(_options);

            InitializingFields(eventConsumer, eventConsumerRetryExhausted);

            return Task.Run(() => _eventSender.StartBackgroundTasks(cancellationToken), cancellationToken);
        }

        private void EnsureNotStarted()
        {
            lock (_startLock)
            {
                if (IsStarted) throw new Exception($"{nameof(DeferredTaskManagerService<T>)} has already been launched.");
                IsStarted = true;
            }
        }

        private void ValidateOptions(DeferredTaskManagerOptions<T> options)
        {
            if (options == null)
                throw new ArgumentNullException(nameof(options));

            var context = new ValidationContext(options, serviceProvider: null, items: null);
            Validator.ValidateObject(options, context, true);
        }

        private void InitializingFields(Func<List<T>, CancellationToken, Task> eventConsumer,
            Func<List<T>, Exception, int, CancellationToken, Task>? eventConsumerRetryExhausted = null)
        {
            _options.EventConsumer = eventConsumer;
            _options.RetryOptions.EventConsumerRetryExhausted = eventConsumerRetryExhausted;
        }

        private void Add(Action action, bool sendEvents)
        {
            action();

            if (sendEvents)
                SendEvents();
        }
    };
}