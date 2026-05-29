using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTM
{
    /// <inheritdoc/>
    public class QueueStrategy<T> : IStorageStrategy<T>
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
            var newQueue = new ConcurrentQueue<T>();

            var oldQueue = Interlocked.Exchange(ref _queue, newQueue);
                
            var list = new List<T>(oldQueue.Count);

            while (oldQueue.TryDequeue(out var item))
            {
                list.Add(item);
            }

            return list;
        }
    }
}
