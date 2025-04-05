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
        private bool _isStarted = false;

        public DeferredTaskManagerService(DeferredTaskManagerOptions<T> options, IEventStorage<T> eventStorage, IEventSender<T> eventSender, IPoolPubSub pubSub)
        {
            _options = options;
            _eventStorage = eventStorage;
            _eventSender = eventSender;
            _pubSub = pubSub;
        }

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
        #endregion

        #region Implemented methods IPubSub
        /// <inheritdoc/>
        public int FreePoolCount => _pubSub.SubscribersCount;
        /// <inheritdoc/>
        public virtual void SendEvents() => _pubSub.SendEvents();
        /// <inheritdoc/>
        public int EmployedPoolCount => _options.PoolSize - _pubSub.SubscribersCount;
        #endregion

        /// <inheritdoc/>
        public virtual Task StartAsync(DeferredTaskManagerOptions<T> options, CancellationToken cancellationToken = default)
        {
            EnsureNotStarted();

            ValidateOptions(options);

            InitializingFields(options);

            var tasks = _eventSender.CreateBackgroundTasks(cancellationToken);

            return Task.WhenAll(tasks);
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

        private void InitializingFields(DeferredTaskManagerOptions<T> options)
        {
            _options.Update(options);
            _eventStorage.InitializeCollectionStrategy(options.CollectionType);
        }

        private void Add(Action action, bool sendEvents)
        {
            action();

            if (sendEvents)
                SendEvents();
        }

        public void InitializeCollectionStrategy(CollectionType type)
        {
            throw new NotImplementedException();
        }
    };
}