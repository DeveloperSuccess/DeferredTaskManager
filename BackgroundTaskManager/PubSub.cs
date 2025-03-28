using BTM.Extensions;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace BTM
{
    internal class PubSub
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _subscribers = new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>();
        private readonly object _lockSubscribers = new object();

        internal int SubscribersCount => _subscribers.Count;

        internal void SendEvents()
        {
            var task = new TaskCompletionSource<bool>();

            lock (_lockSubscribers)
            {
                var subscriber = _subscribers.FirstOrDefault();

                if (subscriber.Key != Guid.Empty)
                {
                    Unsubscribe(subscriber.Key);
                }

                task = subscriber.Value;
            }

            task?.TrySetResult(true);
        }

        internal void Subscribe(out Guid subscriberKey, out Task<bool> task)
        {
            subscriberKey = Guid.NewGuid();

            var taskCompletionSource = new TaskCompletionSource<bool>();

            _subscribers.AddOrUpdate(subscriberKey, taskCompletionSource);

            task = taskCompletionSource.Task;
        }

        internal void Unsubscribe(Guid subscriberKey)
        {
            lock (_lockSubscribers)
            {
                _subscribers.TryRemove(subscriberKey, out var subscriber);
            }
        }
    }
}
