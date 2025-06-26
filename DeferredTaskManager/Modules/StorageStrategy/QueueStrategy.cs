using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DTM
{
    public class QueueStrategy<T> : IStorageStrategy<T>
    {
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        public void Add(T item) => _queue.Enqueue(item);
        public IEnumerable<T> GetItems() => _queue;
        public int Count => _queue.Count;
        public bool IsEmpty => _queue.IsEmpty;
        public void Clear() => _queue.Clear();
    }
}
