using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DTM
{
    /// <inheritdoc/>
    public class BagStrategy<T> : IStorageStrategy<T>
    {
        /// <inheritdoc/>
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();
        /// <inheritdoc/>
        public void Add(T item) => _bag.Add(item);
        /// <inheritdoc/>
        public IEnumerable<T> GetItems() => _bag;
        /// <inheritdoc/>
        public int Count => _bag.Count;
        /// <inheritdoc/>
        public bool IsEmpty => _bag.IsEmpty;
        /// <inheritdoc/>
        public void Clear() => _bag.Clear();
    }
}
