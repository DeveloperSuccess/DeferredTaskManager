using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DTM
{
    /// <inheritdoc/>
    public class QueueStorage<T> : IQueueStorage<T>
    {
        /// <inheritdoc/>
        public ConcurrentQueue<T> _queue = new ConcurrentQueue<T>();
        /// <inheritdoc/>
        public void Add(T item) => _queue.Enqueue(item);
        /// <inheritdoc/>
        public int Count => _queue.Count;
        /// <inheritdoc/>
        public bool IsEmpty => _queue.IsEmpty;
        /// <inheritdoc/>                
        public List<T> ExtractAll()
        {
            var list = new List<T>();

            while (_queue.TryDequeue(out var item))
            {
                list.Add(item);
            }

            return list;
        }
    }
}
