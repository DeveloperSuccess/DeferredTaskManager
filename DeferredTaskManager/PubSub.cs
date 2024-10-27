using System.Collections.Concurrent;

namespace DeferredTaskManager
{
    internal class PubSub
    {
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _subscribers = new ConcurrentDictionary<Guid, TaskCompletionSource<bool>>();

        public int SubscribersCount => _subscribers.Count;

        public void SendEvents()
        {
            var subscriber = _subscribers.FirstOrDefault();

            if (subscriber.Key != Guid.Empty)
            {
                subscriber.Value.TrySetResult(true);

                Unsubscribe(subscriber.Key);
            }
        }

        public void Subscribe(out Guid subscriberKey, out Task<bool> task)
        {
            subscriberKey = Guid.NewGuid();

            var taskCompletionSource = new TaskCompletionSource<bool>();

            _subscribers.AddOrUpdate(subscriberKey, taskCompletionSource);

            task = taskCompletionSource.Task;
        }

        public void Unsubscribe(Guid subscriberKey)
        {
            _subscribers.TryRemove(subscriberKey, out var subscriber);
        }
    }
}
