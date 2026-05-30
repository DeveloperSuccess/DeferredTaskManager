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
        public List<T> ExtractAll()
        {
            var newBag = new ConcurrentBag<T>();

            // Атомарно меняем ссылку
            var oldBag = Interlocked.Exchange(ref _bag, newBag);

            // Вызываем .ToList() на старом баге. 
            // Это безопасно, так как никто больше не может в него писать.
            return oldBag.ToList();
        }
    }
}
