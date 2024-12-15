using System.Collections.Concurrent;

namespace DeferredTaskManager.CollectionStrategy
{
    public class QueueStrategy<T> : ICollectionStrategy<T>
    {
        private readonly ConcurrentQueue<T> _queue = new();
        public void Add(T item) => _queue.Enqueue(item);
        public IEnumerable<T> GetItems() => _queue;
        public int Count => _queue.Count;
        public bool IsEmpty => _queue.IsEmpty;
        public void Clear() => _queue.Clear();
    }
}
