using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;

namespace DTM
{

    internal class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly object _startLock = new object();
        private readonly IPoolPubSub _pubSub;
        private readonly IEventStorage<T> _eventStorage;
        private readonly IEventSender<T> _eventSender;

        private readonly DeferredTaskManagerOptions<T> _options;
        private bool _isStarted = false;

        public DeferredTaskManagerService(IOptions<DeferredTaskManagerOptions<T>> options, IEventStorage<T> eventStorage, IEventSender<T> eventSender, IPoolPubSub pubSub)
        {
            _options = options.Value;
            _eventStorage = eventStorage;
            _eventSender = eventSender;
            _pubSub = pubSub;
        }

        #region Implemented methods IEventStorage

        public int Count => _eventStorage.Count;

        public bool IsEmpty => _eventStorage.IsEmpty;

        public virtual void Add(T @event, bool sendEvents = true) => Add(() => _eventStorage.Add(@event), sendEvents);

        public virtual void Add(IEnumerable<T> events, bool sendEvents = true) => Add(() => _eventStorage.Add(events), sendEvents);

        public virtual List<T> GetEventsAndClearStorage() => _eventStorage.GetEventsAndClearStorage();
        #endregion

        #region Implemented methods IPubSub

        public int FreePoolCount => _pubSub.SubscribersCount;

        public virtual void SendEvents() => _pubSub.SendEvents();

        public int EmployedPoolCount => _options.PoolSize - _pubSub.SubscribersCount;
        #endregion


        public virtual Task StartAsync(Func<List<T>, CancellationToken, Task> eventConsumer,
            Func<List<T>, Exception, CancellationToken, Task>? eventConsumerRetryExhausted = null,
            CancellationToken cancellationToken = default) =>
            Initializing(eventConsumer, eventConsumerRetryExhausted, cancellationToken);


        public virtual Task StartAsync(Func<List<T>, CancellationToken, Task> eventConsumer,
            CancellationToken cancellationToken = default) =>
            Initializing(eventConsumer, cancellationToken: cancellationToken);

        private Task Initializing(Func<List<T>, CancellationToken, Task> eventConsumer,
            Func<List<T>, Exception, CancellationToken, Task>? eventConsumerRetryExhausted = null,
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
                if (_isStarted) throw new Exception($"{nameof(DeferredTaskManagerService<T>)} has already been launched.");
                _isStarted = true;
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
            Func<List<T>, Exception, CancellationToken, Task>? eventConsumerRetryExhausted = null)
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