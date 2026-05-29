using System;
using System.Buffers;
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
        public ArraySegment<T> ExtractAll()
        {
            var newQueue = new ConcurrentQueue<T>();
            var oldQueue = Interlocked.Exchange(ref _queue, newQueue);

            int count = oldQueue.Count;
            if (count == 0)
            {
                return ArraySegment<T>.Empty;
            }

            var sharedArray = ArrayPool<T>.Shared.Rent(count);
            int actualCount = 0;

            while (oldQueue.TryDequeue(out var item))
            {
                sharedArray[actualCount] = item;
                actualCount++;
            }

            return new ArraySegment<T>(sharedArray, 0, actualCount);
        }
    }
}
