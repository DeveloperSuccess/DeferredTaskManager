using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DTM
{
    /// <inheritdoc/>
    public class QueueStrategy<T> : IStorageStrategy<T>
    {
        /// <inheritdoc/>
        private readonly ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        /// <inheritdoc/>
        public void Add(T item) => _queue.Enqueue(item);
        /// <inheritdoc/>
        public IEnumerable<T> GetItems() => _queue;
        /// <inheritdoc/>
        public int Count => _queue.Count;
        /// <inheritdoc/>
        public bool IsEmpty => _queue.IsEmpty;
        /// <inheritdoc/>
        public void Clear() => _queue.Clear();
    }
}
