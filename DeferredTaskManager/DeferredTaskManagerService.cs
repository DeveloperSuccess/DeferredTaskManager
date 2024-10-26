using System.Collections.Concurrent;

namespace DeferringTasksManager
{
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();
        private readonly object _lockSubscribers = new object();
        private readonly ReaderWriterLockSlim _lockBag = new();
        private readonly int _taskPoolSize;
        private readonly PubSub _pubSub = new PubSub();
        private Func<IEnumerable<T>, CancellationToken, Task> _taskFactory;

        public DeferredTaskManagerService(int taskPoolSize = 1000)
        {
            _taskPoolSize = taskPoolSize;
        }

        public void Add(T @event)
        {
            _lockBag.EnterReadLock();

            try
            {
                _bag.Add(@event);
            }
            finally
            {
                _lockBag.ExitReadLock();
            }

            lock (_lockSubscribers)
            {
                _pubSub.SendEvents();
            }
        }

        public void Add(IEnumerable<T> events)
        {
            _lockBag.EnterReadLock();

            try
            {
                foreach (var ev in events)
                    _bag.Add(ev);
            }
            finally
            {
                _lockBag.ExitReadLock();
            }

            lock (_lockSubscribers)
            {
                _pubSub.SendEvents();
            }
        }

        private List<T> GetAndClearBag()
        {
            List<T> result;

            _lockBag.EnterWriteLock();

            try
            {
                result = _bag.ToList();

                _bag.Clear();
            }
            finally
            {
                _lockBag.ExitWriteLock();
            }

            return result;
        }

        public Task StartAsync(Func<IEnumerable<T>, CancellationToken, Task> taskFactory, CancellationToken cancellationToken)
        {
            _taskFactory = taskFactory;

            var taskPool = new List<Task>();

            for (int i = 0; i < _taskPoolSize; i++)
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

                if (_bag.IsEmpty)
                {
                    var access = await task.WaitAsync(cancellationToken);
                }
                else
                {
                    lock (_lockSubscribers)
                    {
                        _pubSub.Unsubscribe(subscriberKey);
                    }
                }

                try
                {
                    var events = GetAndClearBag();

                    if (events.Count == 0) continue;

                    await _taskFactory(events, cancellationToken);
                }
                catch
                {

                }
            }
        }
    }
}
