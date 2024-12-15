using System.Collections.Generic;

namespace DeferredTaskManager.CollectionStrategy
{
    internal interface ICollectionStrategy<T>
    {
        void Add(T item);
        IEnumerable<T> GetItems();
        int Count { get; }
        bool IsEmpty { get; }
        void Clear();
    }
}
