using System.Collections.Generic;

namespace BTM.CollectionStrategy
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
