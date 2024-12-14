using System.Collections.Concurrent;

namespace DeferredTaskManager
{
    public class DeferredTaskManagerService<T> : IDeferredTaskManagerService<T>
    {
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();
        private readonly ReaderWriterLockSlim _lockBag = new();
        private readonly PubSub _pubSub = new PubSub();

        public int TaskCount => _bag.Count;
        public int SubscribersCount => _pubSub.SubscribersCount;

        private Func<List<T>, CancellationToken, Task> _taskFactory;
        private int _retry;
        int _millisecondsRetryDelay;

        public DeferredTaskManagerService()
        {

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

            _pubSub.SendEvents();
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

            _pubSub.SendEvents();
        }

        private List<T> GetAndClearBag()
        {
            List<T> result;

            _lockBag.EnterWriteLock();

            try
            {
                result = _bag.Reverse().ToList();

                _bag.Clear();
            }
            finally
            {
                _lockBag.ExitWriteLock();
            }

            return result;
        }

        public Task StartAsync(Func<List<T>, CancellationToken, Task> taskFactory, int taskPoolSize = 1000, int retry = 0, int millisecondsRetryDelay = 100, CancellationToken cancellationToken = default)
        {
            _taskFactory = taskFactory;

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

                if (_bag.IsEmpty)
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
