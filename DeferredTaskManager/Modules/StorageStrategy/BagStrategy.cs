using System.Collections.Concurrent;
using System.Collections.Generic;

namespace DTM
{
    public class BagStrategy<T> : IStorageStrategy<T>
    {
        private readonly ConcurrentBag<T> _bag = new ConcurrentBag<T>();
        public void Add(T item) => _bag.Add(item);
        public IEnumerable<T> GetItems() => _bag;
        public int Count => _bag.Count;
        public bool IsEmpty => _bag.IsEmpty;
        public void Clear() => _bag.Clear();
    }
}
