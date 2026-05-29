using System;
using System.Buffers;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DTM
{
    /// <inheritdoc/>
    public class BagStrategy<T> : IStorageStrategy<T>
    {
        /// <inheritdoc/>
        public ConcurrentBag<T> _bag = new ConcurrentBag<T>();
        /// <inheritdoc/>
        public void Add(T item) => _bag.Add(item);
        /// <inheritdoc/>
        public int Count => _bag.Count;
        /// <inheritdoc/>
        public bool IsEmpty => _bag.IsEmpty;
        /// <inheritdoc/>
        public ArraySegment<T> ExtractAll()
        {
            var newBag = new ConcurrentBag<T>();
            var oldBag = Interlocked.Exchange(ref _bag, newBag);

            int count = oldBag.Count;
            if (count == 0)
            {
                return ArraySegment<T>.Empty;
            }

            T[] sharedArray = ArrayPool<T>.Shared.Rent(count);

            try
            {
                oldBag.CopyTo(sharedArray, 0);
            }
            catch (Exception)
            {
                ArrayPool<T>.Shared.Return(sharedArray, clearArray: true);
                throw;
            }

            return new ArraySegment<T>(sharedArray, 0, count);
        }
    }
}
