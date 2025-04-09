using System.Collections.Generic;

namespace DTM.CollectionStrategy
{
    /// <summary>
    /// Select collection type, «Bag» for the Unordered collection of objects (it works faster) or «Queue» for the Ordered collection of objects.
    /// </summary>
    /// <typeparamref name="T"/>
    public interface ICollectionStrategy<T>
    {
        /// <summary>
        /// Add Value in storage
        /// </summary>
        /// <param name="item">Value</param>
        void Add(T item);
        /// <summary>
        /// Get items from storage
        /// </summary>
        IEnumerable<T> GetItems();
        /// <summary>
        /// Count items from storage
        /// </summary>
        int Count { get; }
        /// <summary>
        /// Checks if the storage is empty
        /// </summary>
        bool IsEmpty { get; }
        /// <summary>
        /// Clears the storage
        /// </summary>
        void Clear();
    }
}
